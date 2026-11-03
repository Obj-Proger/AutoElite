namespace AutoElite.Domain.Events;

/// <summary>
/// Raised when a user's email address changes; the login must follow the new address and the
/// new address must be confirmed again.
/// </summary>
/// <param name="UserId">The identifier of the user.</param>
/// <param name="PreviousEmail">The normalized email address before the change.</param>
/// <param name="NewEmail">The normalized email address after the change.</param>
/// <param name="OccurredOnUtc">The UTC time at which the change occurred.</param>
public sealed record UserEmailChangedDomainEvent(Guid UserId, string PreviousEmail, string NewEmail, DateTime OccurredOnUtc) : IDomainEvent;
