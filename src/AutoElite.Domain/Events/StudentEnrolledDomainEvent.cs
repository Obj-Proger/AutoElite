namespace AutoElite.Domain.Events;

/// <summary>Raised when a student joins a study group; the student is added to the group chat and notified.</summary>
/// <param name="GroupId">The identifier of the group.</param>
/// <param name="ContractId">The identifier of the enrolled contract.</param>
/// <param name="StudentId">The identifier of the student.</param>
/// <param name="OccurredOnUtc">The UTC time of enrollment.</param>
public sealed record StudentEnrolledDomainEvent(Guid GroupId, Guid ContractId, Guid StudentId, DateTime OccurredOnUtc) : IDomainEvent;
