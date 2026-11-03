namespace AutoElite.Domain.Events;

/// <summary>Raised when a message is deleted; open chats remove it in real time.</summary>
/// <param name="ChatId">The identifier of the chat.</param>
/// <param name="MessageId">The identifier of the message.</param>
/// <param name="OccurredOnUtc">The UTC time of the deletion.</param>
public sealed record ChatMessageDeletedDomainEvent(Guid ChatId, Guid MessageId, DateTime OccurredOnUtc) : IDomainEvent;
