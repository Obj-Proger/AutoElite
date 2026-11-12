using System.Globalization;
using Microsoft.AspNetCore.SignalR;

namespace AutoElite.API.Realtime;

/// <summary>Sends the updates to the clients of the users connected to <see cref="RealtimeHub"/>, on any device.</summary>
/// <param name="hub">The hub.</param>
internal sealed class HubRealtimeUpdates(IHubContext<RealtimeHub, IRealtimeClient> hub) : IRealtimeUpdates
{
    /// <inheritdoc />
    public Task SendNotificationAsync(Guid recipientId, NotificationUpdate update, CancellationToken cancellationToken) =>
        hub.Clients.User(UserIdOf(recipientId)).NotificationReceived(update);

    /// <inheritdoc />
    public Task SendChatMessageAsync(
        IReadOnlyCollection<Guid> recipientIds, ChatMessageUpdate update, CancellationToken cancellationToken) =>
        recipientIds.Count == 0
            ? Task.CompletedTask
            : hub.Clients.Users([.. recipientIds.Select(UserIdOf)]).ChatMessageReceived(update);

    // The user identifiers of the hub are those of the access tokens (RealtimeUserIdProvider).
    private static string UserIdOf(Guid userId) => userId.ToString("D", CultureInfo.InvariantCulture);
}
