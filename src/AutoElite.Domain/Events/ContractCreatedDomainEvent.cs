namespace AutoElite.Domain.Events;

/// <summary>Raised when a contract is drawn up.</summary>
/// <param name="ContractId">The identifier of the new contract.</param>
/// <param name="StudentId">The identifier of the student.</param>
/// <param name="CourseId">The identifier of the course.</param>
/// <param name="OccurredOnUtc">The UTC time at which the contract was drawn up.</param>
public sealed record ContractCreatedDomainEvent(Guid ContractId, Guid StudentId, Guid CourseId, DateTime OccurredOnUtc) : IDomainEvent;
