namespace AutoElite.Domain.Events;

/// <summary>Raised when a task is created for, or given to, a staff member; the assignee is notified.</summary>
/// <param name="TaskId">The identifier of the task.</param>
/// <param name="AssigneeId">The identifier of the assignee.</param>
/// <param name="DueAtUtc">The deadline.</param>
/// <param name="OccurredOnUtc">The UTC time of the assignment.</param>
public sealed record StaffTaskAssignedDomainEvent(Guid TaskId, Guid AssigneeId, DateTime DueAtUtc, DateTime OccurredOnUtc) : IDomainEvent;
