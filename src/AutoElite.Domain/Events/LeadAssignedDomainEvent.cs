namespace AutoElite.Domain.Events;

/// <summary>Raised when a manager is made responsible for a lead; the manager is notified.</summary>
/// <param name="LeadId">The identifier of the lead.</param>
/// <param name="ManagerId">The identifier of the assigned manager.</param>
/// <param name="OccurredOnUtc">The UTC time of the assignment.</param>
public sealed record LeadAssignedDomainEvent(Guid LeadId, Guid ManagerId, DateTime OccurredOnUtc) : IDomainEvent;
