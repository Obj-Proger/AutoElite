namespace AutoElite.Application.StaffTasks.StartStaffTask;

/// <summary>Starts work on a new task: the assignee, or a manager or administrator who manages it (FR-12.2).</summary>
/// <param name="TaskId">The identifier of the task.</param>
[RequireRoles(UserRole.Administrator, UserRole.Manager, UserRole.DrivingInstructor, UserRole.TheoryInstructor)]
public sealed record StartStaffTaskCommand(Guid TaskId) : ICommand;
