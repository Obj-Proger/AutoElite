namespace AutoElite.Application.StaffTasks.ReopenStaffTask;

/// <summary>
/// Returns a done task to work: the assignee, or a manager or administrator who manages it (FR-12.2). The next
/// occurrence of a repeating task, already created, stays.
/// </summary>
/// <param name="TaskId">The identifier of the task.</param>
[RequireRoles(UserRole.Administrator, UserRole.Manager, UserRole.DrivingInstructor, UserRole.TheoryInstructor)]
public sealed record ReopenStaffTaskCommand(Guid TaskId) : ICommand;
