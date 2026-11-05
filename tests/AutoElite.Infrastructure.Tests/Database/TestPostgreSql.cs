using System.Diagnostics.CodeAnalysis;
using Npgsql;
using Testcontainers.PostgreSql;

namespace AutoElite.Infrastructure.Tests.Database;

/// <summary>
/// The PostgreSQL server the tests create their databases on. When the <see cref="ServerVariable"/> environment
/// variable holds the connection string of a user who may create databases, that server is used: for a PostgreSQL
/// running elsewhere, such as a development server. Otherwise one throwaway container is started for the whole test
/// run (Testcontainers, which needs a local Docker engine, as in CI) and removed when the run ends.
/// </summary>
[SuppressMessage("Security", "CA2100", Justification = "The statements take no input; database names are generated here.")]
internal static class TestPostgreSql
{
    /// <summary>The environment variable with the connection string of the server to use instead of a container.</summary>
    public const string ServerVariable = "AUTOELITE_TEST_POSTGRES";

    private const string NamePrefix = "autoelite_test_";

    private static readonly Lazy<Task<string>> _server = new(StartAsync);

    // Kept for the whole run; the Testcontainers resource reaper removes it when the test process exits.
    private static PostgreSqlContainer? _container;

    /// <summary>Creates an empty database with a new name.</summary>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    /// <returns>The connection string of the new database.</returns>
    public static async Task<string> CreateDatabaseAsync(CancellationToken cancellationToken)
    {
        var server = await _server.Value;
        var name = NamePrefix + Guid.NewGuid().ToString("N");
        await ExecuteAsync(server, $"CREATE DATABASE \"{name}\"", cancellationToken);

        return new NpgsqlConnectionStringBuilder(server) { Database = name }.ConnectionString;
    }

    /// <summary>Drops a database created by <see cref="CreateDatabaseAsync"/>, closing its connections.</summary>
    /// <param name="connectionString">The connection string of the database.</param>
    public static async Task DropDatabaseAsync(string connectionString)
    {
        var server = await _server.Value;
        var name = new NpgsqlConnectionStringBuilder(connectionString).Database;

        NpgsqlConnection.ClearAllPools();
        await ExecuteAsync(server, $"DROP DATABASE IF EXISTS \"{name}\" WITH (FORCE)", CancellationToken.None);
    }

    private static async Task<string> StartAsync()
    {
        var server = Environment.GetEnvironmentVariable(ServerVariable);
        if (!string.IsNullOrWhiteSpace(server))
            return server;

        _container = new PostgreSqlBuilder("postgres:18").Build();
        await _container.StartAsync();

        return _container.GetConnectionString();
    }

    private static async Task ExecuteAsync(string connectionString, string sql, CancellationToken cancellationToken)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new NpgsqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
