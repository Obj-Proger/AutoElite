namespace AutoElite.Application.StaffTasks;

/// <summary>The conditions of the task list; a condition that is <see langword="null"/> does not filter.</summary>
/// <param name="AssigneeId">Only tasks of this staff member.</param>
/// <param name="Status">Only tasks in this status.</param>
/// <param name="OverdueBeforeUtc">Only open tasks whose deadline is earlier than this UTC time, the current time.</param>
/// <param name="Subject">Only tasks about this record.</param>
public sealed record StaffTaskListFilter(
    Guid? AssigneeId, StaffTaskStatus? Status, DateTime? OverdueBeforeUtc, EntityReference? Subject);

/// <summary>A row of the task list.</summary>
/// <param name="Id">The identifier of the task.</param>
/// <param name="Title">The title.</param>
/// <param name="AssigneeId">The staff member responsible.</param>
/// <param name="AssigneeName">The full name of the assignee.</param>
/// <param name="DueAtUtc">The deadline.</param>
/// <param name="Status">The status.</param>
/// <param name="Recurrence">How often the task repeats.</param>
/// <param name="Subject">The record the task is about, if any.</param>
public sealed record StaffTaskListItem(
    Guid Id,
    string Title,
    Guid AssigneeId,
    string AssigneeName,
    DateTime DueAtUtc,
    StaffTaskStatus Status,
    TaskRecurrence Recurrence,
    EntityReference? Subject);

/// <summary>The card of a task.</summary>
/// <param name="Id">The identifier of the task.</param>
/// <param name="Title">The title.</param>
/// <param name="Description">The description, if any.</param>
/// <param name="AssigneeId">The staff member responsible.</param>
/// <param name="AssigneeName">The full name of the assignee.</param>
/// <param name="AssigneeBranchId">The branch of the assignee, which decides which managers manage the task.</param>
/// <param name="DueAtUtc">The deadline.</param>
/// <param name="Status">The status.</param>
/// <param name="Recurrence">How often the task repeats.</param>
/// <param name="Subject">The record the task is about, if any.</param>
/// <param name="CompletedAtUtc">When the task was completed, if it is done.</param>
/// <param name="CreatedAtUtc">When the task was set.</param>
/// <param name="CreatedByName">The full name of the user who set the task, if a user did.</param>
public sealed record StaffTaskDetails(
    Guid Id,
    string Title,
    string? Description,
    Guid AssigneeId,
    string AssigneeName,
    Guid? AssigneeBranchId,
    DateTime DueAtUtc,
    StaffTaskStatus Status,
    TaskRecurrence Recurrence,
    EntityReference? Subject,
    DateTime? CompletedAtUtc,
    DateTime CreatedAtUtc,
    string? CreatedByName);
