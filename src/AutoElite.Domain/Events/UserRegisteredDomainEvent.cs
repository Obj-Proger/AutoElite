namespace AutoElite.Domain.Events;

/// <summary>Raised when a new user is registered.</summary>
/// <param name="UserId">The identifier of the registered user.</param>
/// <param name="Email">The user's normalized email address.</param>
/// <param name="OccurredOnUtc">The UTC time at which the registration occurred.</param>
public sealed record UserRegisteredDomainEvent(Guid UserId, string Email, DateTime OccurredOnUtc) : IDomainEvent;