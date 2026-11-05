namespace AutoElite.Application.StaffTasks.UpdateStaffTask;

/// <summary>
/// Changes the title, description, deadline and linked record of an open task. A new deadline allows a new overdue
/// reminder.
/// </summary>
/// <param name="TaskId">The identifier of the task.</param>
/// <param name="Title">The title.</param>
/// <param name="Description">The description, if any.</param>
/// <param name="DueAtUtc">The deadline in UTC; a changed deadline must be in the future.</param>
/// <param name="SubjectKind">The kind of the record the task is about, if any.</param>
/// <param name="SubjectId">The identifier of that record; given together with the kind.</param>
[RequireRoles(UserRole.Administrator, UserRole.Manager)]
public sealed record UpdateStaffTaskCommand(
    Guid TaskId,
    string? Title,
    string? Description,
    DateTime DueAtUtc,
    EntityKind? SubjectKind,
    Guid? SubjectId) : ICommand;
