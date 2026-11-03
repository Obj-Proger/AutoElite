namespace AutoElite.Domain.Events;

/// <summary>Raised when a message is sent; it is delivered in real time and members who did not mute the chat are notified.</summary>
/// <param name="ChatId">The identifier of the chat.</param>
/// <param name="MessageId">The identifier of the message.</param>
/// <param name="SenderId">The identifier of the sender.</param>
/// <param name="OccurredOnUtc">The UTC time the message was sent.</param>
public sealed record ChatMessageSentDomainEvent(Guid ChatId, Guid MessageId, Guid SenderId, DateTime OccurredOnUtc) : IDomainEvent;
