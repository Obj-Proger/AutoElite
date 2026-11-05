namespace AutoElite.Application.StaffTasks.CompleteStaffTask;

/// <summary>
/// Completes an open task: the assignee, or a manager or administrator who manages it (FR-12.2). A repeating task
/// gets its next occurrence (FR-12.3).
/// </summary>
/// <param name="TaskId">The identifier of the task.</param>
[RequireRoles(UserRole.Administrator, UserRole.Manager, UserRole.DrivingInstructor, UserRole.TheoryInstructor)]
public sealed record CompleteStaffTaskCommand(Guid TaskId) : ICommand;
