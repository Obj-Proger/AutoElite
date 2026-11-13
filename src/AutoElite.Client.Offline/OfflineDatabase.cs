using System.Globalization;
using Microsoft.Data.Sqlite;

namespace AutoElite.Client.Offline;

/// <summary>
/// The SQLite database on the device that keeps what the user last read from the API (<see cref="OfflineCache"/>) and
/// the actions they took without a network (<see cref="OfflineQueue"/>). It holds the data of one user: when another
/// user claims the device, the data of the previous one is forgotten. One connection serves every operation, one at a
/// time, away from the thread of the caller.
/// </summary>
public sealed class OfflineDatabase : IDisposable
{
    // The version of the schema, kept in the user_version of the database; a later version adds its steps to Migrate.
    private const long SchemaVersion = 1;

    private const string OwnerSetting = "owner";

    private readonly string _path;
    private readonly SqliteConnection _connection;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private bool _isOpen;

    /// <summary>Initializes a new instance of the <see cref="OfflineDatabase"/> class.</summary>
    /// <param name="path">The file of the database, created on first use.</param>
    public OfflineDatabase(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        _path = path;

        // Without pooling, disposing of the connection closes the file.
        var connectionString = new SqliteConnectionStringBuilder { DataSource = path, Pooling = false };
        _connection = new SqliteConnection(connectionString.ToString());
    }

    /// <summary>
    /// Makes the data on the device the data of the signed-in user: when it belongs to another user, it is forgotten
    /// first, so that nobody sees what another user read and no action is sent on behalf of another user.
    /// </summary>
    /// <param name="userId">The signed-in user.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    /// <returns><see langword="true"/> when the data of another user was forgotten.</returns>
    public Task<bool> ClaimAsync(Guid userId, CancellationToken cancellationToken) =>
        RunAsync(connection =>
        {
            var owner = userId.ToString("D", CultureInfo.InvariantCulture);
            using var transaction = connection.BeginTransaction();
            var previous = Scalar(connection, "SELECT value FROM settings WHERE key = $key", ("$key", OwnerSetting)) as string;
            if (previous == owner)
                return false;

            var forgotten = previous is not null || HasData(connection);
            DeleteAll(connection);
            Execute(connection, "INSERT INTO settings (key, value) VALUES ($key, $value)", ("$key", OwnerSetting), ("$value", owner));
            transaction.Commit();

            return forgotten;
        }, cancellationToken);

    /// <summary>
    /// Forgets everything: what the user read, the actions not yet sent and the owner of the data. The application does
    /// it when the user signs out.
    /// </summary>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    public Task ForgetAsync(CancellationToken cancellationToken) =>
        RunAsync(connection =>
        {
            using var transaction = connection.BeginTransaction();
            DeleteAll(connection);
            transaction.Commit();

            return true;
        }, cancellationToken);

    /// <inheritdoc />
    public void Dispose()
    {
        _connection.Dispose();
        _lock.Dispose();
    }

    /// <summary>Runs work on the connection, after every other piece of work has finished.</summary>
    /// <typeparam name="T">The type of the result.</typeparam>
    /// <param name="work">The work, which may use a transaction of its own.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    /// <returns>The result of the work.</returns>
    internal async Task<T> RunAsync<T>(Func<SqliteConnection, T> work, CancellationToken cancellationToken)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            return await Task.Run(
                () =>
                {
                    Open();

                    return work(_connection);
                },
                cancellationToken);
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <summary>Runs a statement.</summary>
    /// <param name="connection">The connection.</param>
    /// <param name="sql">The statement, with its parameters as <c>$name</c>.</param>
    /// <param name="parameters">The values of the parameters.</param>
    /// <returns>The number of rows the statement changed.</returns>
    internal static int Execute(SqliteConnection connection, string sql, params (string Name, object? Value)[] parameters)
    {
        using var command = Command(connection, sql, parameters);

        return command.ExecuteNonQuery();
    }

    /// <summary>Runs a query that gives one value.</summary>
    /// <param name="connection">The connection.</param>
    /// <param name="sql">The query, with its parameters as <c>$name</c>.</param>
    /// <param name="parameters">The values of the parameters.</param>
    /// <returns>The first column of the first row, or <see langword="null"/> when there is no row.</returns>
    internal static object? Scalar(SqliteConnection connection, string sql, params (string Name, object? Value)[] parameters)
    {
        using var command = Command(connection, sql, parameters);

        return command.ExecuteScalar();
    }

    /// <summary>Creates a command of a statement or a query; the caller disposes of it.</summary>
    /// <param name="connection">The connection.</param>
    /// <param name="sql">The statement or query, with its parameters as <c>$name</c>.</param>
    /// <param name="parameters">The values of the parameters.</param>
    /// <returns>The command.</returns>
    internal static SqliteCommand Command(SqliteConnection connection, string sql, params (string Name, object? Value)[] parameters)
    {
        var command = connection.CreateCommand();
        command.CommandText = sql;
        foreach (var (name, value) in parameters)
            command.Parameters.AddWithValue(name, value ?? DBNull.Value);

        return command;
    }

    private static bool HasData(SqliteConnection connection) =>
        Scalar(connection, "SELECT EXISTS (SELECT 1 FROM snapshots) OR EXISTS (SELECT 1 FROM pending_actions)") is 1L;

    private static void DeleteAll(SqliteConnection connection)
    {
        Execute(connection, "DELETE FROM snapshots");
        Execute(connection, "DELETE FROM pending_actions");
        Execute(connection, "DELETE FROM settings");
    }

    private static void Migrate(SqliteConnection connection)
    {
        var version = (long)Scalar(connection, "PRAGMA user_version")!;
        if (version == SchemaVersion)
            return;

        if (version > SchemaVersion)
        {
            throw new InvalidOperationException(
                "The offline database was made by a later version of the application; reinstall the later version.");
        }

        using var transaction = connection.BeginTransaction();
        Execute(connection, """
            CREATE TABLE settings (
                key TEXT NOT NULL PRIMARY KEY,
                value TEXT NOT NULL
            ) WITHOUT ROWID;

            CREATE TABLE snapshots (
                key TEXT NOT NULL PRIMARY KEY,
                content TEXT NOT NULL,
                saved_at INTEGER NOT NULL
            ) WITHOUT ROWID;

            CREATE TABLE pending_actions (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                content TEXT NOT NULL,
                queued_at INTEGER NOT NULL,
                attempts INTEGER NOT NULL DEFAULT 0,
                rejected_with_status INTEGER NULL
            );

            PRAGMA user_version = 1;
            """);
        transaction.Commit();
    }

    private void Open()
    {
        if (_isOpen)
            return;

        if (Path.GetDirectoryName(Path.GetFullPath(_path)) is { Length: > 0 } directory)
            Directory.CreateDirectory(directory);

        _connection.Open();
        Migrate(_connection);
        _isOpen = true;
    }
}
