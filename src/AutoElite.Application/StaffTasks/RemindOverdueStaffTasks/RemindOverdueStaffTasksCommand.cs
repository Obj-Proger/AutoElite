namespace AutoElite.Application.StaffTasks.RemindOverdueStaffTasks;

/// <summary>
/// Marks open tasks past their deadline as overdue, which notifies their assignees once per deadline (FR-12.4).
/// A scheduled job sends it every few minutes; runs must not overlap.
/// </summary>
[SystemRequest]
public sealed record RemindOverdueStaffTasksCommand : ICommand;
