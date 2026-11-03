namespace AutoElite.Domain.Events;

/// <summary>Raised when a user leaves or is removed from a group chat; the real-time subscription ends.</summary>
/// <param name="ChatId">The identifier of the chat.</param>
/// <param name="UserId">The identifier of the user.</param>
/// <param name="OccurredOnUtc">The UTC time of the change.</param>
public sealed record ChatMemberLeftDomainEvent(Guid ChatId, Guid UserId, DateTime OccurredOnUtc) : IDomainEvent;
