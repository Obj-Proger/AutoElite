namespace AutoElite.Client.Offline;

/// <summary>What one synchronization of the queue did.</summary>
/// <param name="Sent">How many actions the API accepted.</param>
/// <param name="Rejected">How many actions the API refused; they stay in the queue for the user to see.</param>
/// <param name="IsComplete">
/// <see langword="true"/> when no action waits any more; <see langword="false"/> when the API could not be reached
/// and the remaining actions wait for the next synchronization.
/// </param>
public sealed record SynchronizationResult(int Sent, int Rejected, bool IsComplete);
