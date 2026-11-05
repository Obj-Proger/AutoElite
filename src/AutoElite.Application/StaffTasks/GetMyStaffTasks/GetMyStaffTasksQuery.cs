using AutoElite.Application.Common.Pagination;

namespace AutoElite.Application.StaffTasks.GetMyStaffTasks;

/// <summary>Reads a page of the caller's own tasks, the nearest deadline first.</summary>
/// <param name="Status">Only tasks in this status.</param>
/// <param name="OverdueOnly">Only open tasks past their deadline.</param>
/// <param name="Page">The page number, starting from 1.</param>
/// <param name="PageSize">The number of tasks per page.</param>
[RequireRoles(UserRole.Administrator, UserRole.Manager, UserRole.DrivingInstructor, UserRole.TheoryInstructor)]
public sealed record GetMyStaffTasksQuery(
    StaffTaskStatus? Status = null,
    bool OverdueOnly = false,
    int Page = 1,
    int PageSize = PageRequest.DefaultPageSize) : IQuery<PagedList<StaffTaskListItem>>, IPagedQuery;
