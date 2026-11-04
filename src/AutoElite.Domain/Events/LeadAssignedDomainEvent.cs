namespace AutoElite.Domain.Events;

/// <summary>Raised when a manager is made responsible for a lead; the manager is notified unless they took it themselves.</summary>
/// <param name="LeadId">The identifier of the lead.</param>
/// <param name="ManagerId">The identifier of the assigned manager.</param>
/// <param name="AssignedByUserId">The user who made the assignment, or <see langword="null"/> for the system.</param>
/// <param name="OccurredOnUtc">The UTC time of the assignment.</param>
public sealed record LeadAssignedDomainEvent(Guid LeadId, Guid ManagerId, Guid? AssignedByUserId, DateTime OccurredOnUtc) : IDomainEvent;
