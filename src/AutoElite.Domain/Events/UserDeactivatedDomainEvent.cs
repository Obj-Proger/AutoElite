namespace AutoElite.Domain.Events;

/// <summary>Raised when a user is deactivated; the user's sessions must be revoked in response.</summary>
/// <param name="UserId">The identifier of the deactivated user.</param>
/// <param name="OccurredOnUtc">The UTC time at which the deactivation occurred.</param>
public sealed record UserDeactivatedDomainEvent(Guid UserId, DateTime OccurredOnUtc) : IDomainEvent;
