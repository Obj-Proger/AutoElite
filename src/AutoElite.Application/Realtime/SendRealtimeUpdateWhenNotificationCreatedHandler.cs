using AutoElite.Application.Common.Events;

namespace AutoElite.Application.Realtime;

/// <summary>Tells the connected clients of a user about a new notification, for the badge of the bell.</summary>
/// <param name="realtimeUpdates">The channel to the connected clients.</param>
internal sealed class SendRealtimeUpdateWhenNotificationCreatedHandler(IRealtimeUpdates realtimeUpdates)
    : IDomainEventHandler<NotificationCreatedDomainEvent>
{
    public Task Handle(NotificationCreatedDomainEvent domainEvent, CancellationToken cancellationToken) =>
        realtimeUpdates.SendNotificationAsync(
            domainEvent.RecipientId, new NotificationUpdate(domainEvent.NotificationId, domainEvent.Type), cancellationToken);
}
