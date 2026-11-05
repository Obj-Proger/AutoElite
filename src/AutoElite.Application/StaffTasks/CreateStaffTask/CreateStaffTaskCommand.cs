namespace AutoElite.Application.StaffTasks.CreateStaffTask;

/// <summary>Gives a staff member a task with a deadline, optionally about a record and repeating (FR-12.1, FR-12.3).</summary>
/// <param name="Title">The title.</param>
/// <param name="Description">The description, if any.</param>
/// <param name="AssigneeId">The staff member responsible; a manager can choose only staff of their own branch.</param>
/// <param name="DueAtUtc">The deadline in UTC; it must be in the future.</param>
/// <param name="SubjectKind">The kind of the record the task is about, if any, for example a lead.</param>
/// <param name="SubjectId">The identifier of that record; given together with the kind.</param>
/// <param name="Recurrence">How often the task repeats.</param>
[RequireRoles(UserRole.Administrator, UserRole.Manager)]
public sealed record CreateStaffTaskCommand(
    string? Title,
    string? Description,
    Guid AssigneeId,
    DateTime DueAtUtc,
    EntityKind? SubjectKind,
    Guid? SubjectId,
    TaskRecurrence Recurrence) : ICommand<Guid>;
