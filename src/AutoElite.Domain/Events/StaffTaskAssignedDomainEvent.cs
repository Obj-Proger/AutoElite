namespace AutoElite.Domain.Events;

/// <summary>
/// Raised when a task is created for, or given to, a staff member, including the next occurrence of a repeating task;
/// the assignee is notified unless they assigned it themselves.
/// </summary>
/// <param name="TaskId">The identifier of the task.</param>
/// <param name="AssigneeId">The identifier of the assignee.</param>
/// <param name="AssignedByUserId">
/// The user who created or reassigned the task, or completed the previous occurrence; <see langword="null"/> for the
/// system.
/// </param>
/// <param name="DueAtUtc">The deadline.</param>
/// <param name="OccurredOnUtc">The UTC time of the assignment.</param>
public sealed record StaffTaskAssignedDomainEvent(
    Guid TaskId, Guid AssigneeId, Guid? AssignedByUserId, DateTime DueAtUtc, DateTime OccurredOnUtc) : IDomainEvent;
