namespace AutoElite.Domain.Events;

/// <summary>Raised when a student leaves a study group; the student is removed from the group chat.</summary>
/// <param name="GroupId">The identifier of the group.</param>
/// <param name="ContractId">The identifier of the contract.</param>
/// <param name="StudentId">The identifier of the student.</param>
/// <param name="OccurredOnUtc">The UTC time the student left.</param>
public sealed record StudentWithdrawnDomainEvent(Guid GroupId, Guid ContractId, Guid StudentId, DateTime OccurredOnUtc) : IDomainEvent;
