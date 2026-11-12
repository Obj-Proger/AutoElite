namespace AutoElite.Application.Common.Abstractions;

/// <summary>
/// Tells the connected clients of users at once that something changed for them, so that they do not have to ask the
/// API again and again. The updates are signals with identifiers: a client reads what changed through the API, which
/// checks access as for any request. Delivery is best effort; a client that is not connected misses them and reads
/// the current state when it connects.
/// </summary>
public interface IRealtimeUpdates
{
    /// <summary>Tells a user about a new notification.</summary>
    /// <param name="recipientId">The user.</param>
    /// <param name="update">The notification.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    Task SendNotificationAsync(Guid recipientId, NotificationUpdate update, CancellationToken cancellationToken);

    /// <summary>Tells the members of a chat about a new message.</summary>
    /// <param name="recipientIds">The members.</param>
    /// <param name="update">The message.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    Task SendChatMessageAsync(IReadOnlyCollection<Guid> recipientIds, ChatMessageUpdate update, CancellationToken cancellationToken);
}

/// <summary>A new notification of a user.</summary>
/// <param name="NotificationId">The notification.</param>
/// <param name="Type">What the notification is about.</param>
public sealed record NotificationUpdate(Guid NotificationId, NotificationType Type);

/// <summary>A new message in a chat.</summary>
/// <param name="ChatId">The chat.</param>
/// <param name="MessageId">The message.</param>
/// <param name="SenderId">The member who sent it.</param>
public sealed record ChatMessageUpdate(Guid ChatId, Guid MessageId, Guid SenderId);
