using System.Text.Json;
using Microsoft.Data.Sqlite;

namespace AutoElite.Client.Offline;

/// <summary>
/// The actions the user took that wait to be sent to the API, in the order they were taken, kept in the offline
/// database so that they survive a restart of the application. <see cref="OfflineSynchronizer"/> sends them; an action
/// the API refuses stays in the queue, marked as rejected, until the user has seen it and dismisses it. An action that
/// this version of the application cannot read, such as one of a later version or one that cannot be decrypted, is
/// dropped. The actions are kept encrypted.
/// </summary>
/// <param name="database">The offline database.</param>
/// <param name="timeProvider">The clock.</param>
public sealed class OfflineQueue(OfflineDatabase database, TimeProvider timeProvider)
{
    private const string Columns = "id, content, queued_at, attempts, rejected_with_status";

    /// <summary>The place of the actions, which binds their encrypted content to the queue.</summary>
    internal const string Place = "pending_actions";

    /// <summary>Occurs when an action is added, sent, rejected or dismissed, on the thread that changed the queue.</summary>
    public event EventHandler? Changed;

    /// <summary>Adds an action at the end of the queue.</summary>
    /// <param name="action">The action.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    /// <returns>The position of the action in the queue.</returns>
    public async Task<long> EnqueueAsync(OfflineAction action, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(action);
        var content = JsonSerializer.Serialize(action, OfflineJsonContext.Default.OfflineAction);
        var queuedAt = timeProvider.GetUtcNow().ToUnixTimeMilliseconds();

        var id = await database.RunAsync(
            connection => (long)OfflineDatabase.Scalar(
                connection,
                "INSERT INTO pending_actions (content, queued_at) VALUES ($content, $queuedAt) RETURNING id",
                ("$content", database.Encrypt(content, Place)),
                ("$queuedAt", queuedAt))!,
            cancellationToken);
        Changed?.Invoke(this, EventArgs.Empty);

        return id;
    }

    /// <summary>Reads the actions in the queue, the earliest first, rejected ones included.</summary>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    /// <returns>The actions.</returns>
    public Task<IReadOnlyList<PendingAction>> ListAsync(CancellationToken cancellationToken) =>
        database.RunAsync<IReadOnlyList<PendingAction>>(
            connection => Read(connection, $"SELECT {Columns} FROM pending_actions ORDER BY id"), cancellationToken);

    /// <summary>
    /// Removes an action from the queue: a rejected one the user has seen, or a waiting one they no longer want sent.
    /// </summary>
    /// <param name="id">The position of the action in the queue.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    /// <returns><see langword="true"/> when the action was in the queue.</returns>
    public Task<bool> DismissAsync(long id, CancellationToken cancellationToken) => RemoveAsync(id, cancellationToken);

    /// <summary>Reads the earliest action that waits to be sent.</summary>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    /// <returns>The action, or <see langword="null"/> when none waits.</returns>
    internal Task<PendingAction?> NextWaitingAsync(CancellationToken cancellationToken) =>
        database.RunAsync<PendingAction?>(
            connection => Read(
                connection,
                $"SELECT {Columns} FROM pending_actions WHERE rejected_with_status IS NULL ORDER BY id")
                .FirstOrDefault(),
            cancellationToken);

    /// <summary>Removes an action the API has accepted.</summary>
    /// <param name="id">The position of the action in the queue.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    /// <returns>A task that completes when the action is removed.</returns>
    internal Task CompleteAsync(long id, CancellationToken cancellationToken) => RemoveAsync(id, cancellationToken);

    /// <summary>Counts a failed attempt to send an action because the API could not be reached.</summary>
    /// <param name="id">The position of the action in the queue.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    /// <returns>A task that completes when the attempt is counted.</returns>
    internal Task RecordAttemptAsync(long id, CancellationToken cancellationToken) =>
        database.RunAsync(
            connection => OfflineDatabase.Execute(
                connection, "UPDATE pending_actions SET attempts = attempts + 1 WHERE id = $id", ("$id", id)),
            cancellationToken);

    /// <summary>Marks an action as refused by the API, so that it is not sent again.</summary>
    /// <param name="id">The position of the action in the queue.</param>
    /// <param name="statusCode">The HTTP status of the refusal.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    /// <returns>A task that completes when the action is marked.</returns>
    internal async Task RejectAsync(long id, int statusCode, CancellationToken cancellationToken)
    {
        await database.RunAsync(
            connection => OfflineDatabase.Execute(
                connection,
                "UPDATE pending_actions SET rejected_with_status = $status WHERE id = $id",
                ("$status", statusCode),
                ("$id", id)),
            cancellationToken);
        Changed?.Invoke(this, EventArgs.Empty);
    }

    private async Task<bool> RemoveAsync(long id, CancellationToken cancellationToken)
    {
        var removed = await database.RunAsync(
            connection => OfflineDatabase.Execute(connection, "DELETE FROM pending_actions WHERE id = $id", ("$id", id)) > 0,
            cancellationToken);
        if (removed)
            Changed?.Invoke(this, EventArgs.Empty);

        return removed;
    }

    // Reads the actions of a query and drops those that cannot be read.
    private List<PendingAction> Read(SqliteConnection connection, string query)
    {
        var actions = new List<PendingAction>();
        var unreadable = new List<long>();
        using (var command = OfflineDatabase.Command(connection, query))
        using (var reader = command.ExecuteReader())
        {
            while (reader.Read())
            {
                var id = reader.GetInt64(0);
                if (database.Decrypt(reader.GetFieldValue<byte[]>(1), Place) is not { } content
                    || Deserialize(content) is not { } action)
                {
                    unreadable.Add(id);
                    continue;
                }

                actions.Add(new PendingAction(
                    id,
                    action,
                    DateTimeOffset.FromUnixTimeMilliseconds(reader.GetInt64(2)),
                    reader.GetInt32(3),
                    reader.IsDBNull(4) ? null : reader.GetInt32(4)));
            }
        }

        foreach (var id in unreadable)
            OfflineDatabase.Execute(connection, "DELETE FROM pending_actions WHERE id = $id", ("$id", id));

        return actions;
    }

    private static OfflineAction? Deserialize(string content)
    {
        try
        {
            return JsonSerializer.Deserialize(content, OfflineJsonContext.Default.OfflineAction);
        }
        catch (Exception exception) when (exception is JsonException or NotSupportedException)
        {
            return null;
        }
    }
}
