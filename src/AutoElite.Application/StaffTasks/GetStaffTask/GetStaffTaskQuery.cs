namespace AutoElite.Application.StaffTasks.GetStaffTask;

/// <summary>Reads the card of a task: for its assignee, or for a manager or administrator who manages it.</summary>
/// <param name="TaskId">The identifier of the task.</param>
[RequireRoles(UserRole.Administrator, UserRole.Manager, UserRole.DrivingInstructor, UserRole.TheoryInstructor)]
public sealed record GetStaffTaskQuery(Guid TaskId) : IQuery<StaffTaskDetails>;
