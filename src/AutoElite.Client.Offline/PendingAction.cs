namespace AutoElite.Client.Offline;

/// <summary>An action in the queue: waiting to be sent, or refused by the API until the user dismisses it.</summary>
/// <param name="Id">The position of the action in the queue.</param>
/// <param name="Action">The action.</param>
/// <param name="QueuedAt">When the user took the action.</param>
/// <param name="Attempts">How many times sending it failed because the API could not be reached.</param>
/// <param name="RejectedWithStatusCode">
/// The HTTP status with which the API refused the action, such as 404 when what it was about no longer exists or 409
/// when it conflicts with a change made meanwhile; <see langword="null"/> while it waits.
/// </param>
public sealed record PendingAction(long Id, OfflineAction Action, DateTimeOffset QueuedAt, int Attempts, int? RejectedWithStatusCode)
{
    /// <summary>Gets a value indicating whether the API refused the action.</summary>
    public bool IsRejected => RejectedWithStatusCode is not null;
}
