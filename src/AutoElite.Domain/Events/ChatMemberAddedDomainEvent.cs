namespace AutoElite.Domain.Events;

/// <summary>Raised when a user is added to a group chat; the user is notified and subscribed to the chat in real time.</summary>
/// <param name="ChatId">The identifier of the chat.</param>
/// <param name="UserId">The identifier of the added user.</param>
/// <param name="OccurredOnUtc">The UTC time of the change.</param>
public sealed record ChatMemberAddedDomainEvent(Guid ChatId, Guid UserId, DateTime OccurredOnUtc) : IDomainEvent;
