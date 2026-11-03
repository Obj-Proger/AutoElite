namespace AutoElite.Domain.Events;

/// <summary>Raised once when an open task passes its deadline; the assignee is notified (FR-12.4).</summary>
/// <param name="TaskId">The identifier of the task.</param>
/// <param name="AssigneeId">The identifier of the assignee.</param>
/// <param name="DueAtUtc">The missed deadline.</param>
/// <param name="OccurredOnUtc">The UTC time the task was found overdue.</param>
public sealed record StaffTaskOverdueDomainEvent(Guid TaskId, Guid AssigneeId, DateTime DueAtUtc, DateTime OccurredOnUtc) : IDomainEvent;
