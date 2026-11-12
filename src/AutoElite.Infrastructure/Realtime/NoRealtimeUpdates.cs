namespace AutoElite.Infrastructure.Realtime;

/// <summary>
/// The real-time channel of a host without connected clients, such as a worker: the updates go nowhere, and clients
/// read the current state through the API. A host with a channel, such as the API, replaces it with its own.
/// </summary>
internal sealed class NoRealtimeUpdates : IRealtimeUpdates
{
    /// <inheritdoc />
    public Task SendNotificationAsync(Guid recipientId, NotificationUpdate update, CancellationToken cancellationToken) =>
        Task.CompletedTask;

    /// <inheritdoc />
    public Task SendChatMessageAsync(
        IReadOnlyCollection<Guid> recipientIds, ChatMessageUpdate update, CancellationToken cancellationToken) =>
        Task.CompletedTask;
}
