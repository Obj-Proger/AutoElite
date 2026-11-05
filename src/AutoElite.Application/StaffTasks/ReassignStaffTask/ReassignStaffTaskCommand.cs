namespace AutoElite.Application.StaffTasks.ReassignStaffTask;

/// <summary>Gives an open task to another staff member; a manager can choose only staff of their own branch.</summary>
/// <param name="TaskId">The identifier of the task.</param>
/// <param name="AssigneeId">The new assignee.</param>
[RequireRoles(UserRole.Administrator, UserRole.Manager)]
public sealed record ReassignStaffTaskCommand(Guid TaskId, Guid AssigneeId) : ICommand;
