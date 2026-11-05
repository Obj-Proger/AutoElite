using AutoElite.Application.Common.Pagination;

namespace AutoElite.Application.StaffTasks.GetStaffTasks;

/// <summary>
/// Reads a page of the tasks the caller manages, the nearest deadline first: every task for an administrator, the
/// tasks of the staff of the manager's branch for a manager.
/// </summary>
/// <param name="AssigneeId">Only tasks of this staff member.</param>
/// <param name="Status">Only tasks in this status.</param>
/// <param name="OverdueOnly">Only open tasks past their deadline.</param>
/// <param name="SubjectKind">Only tasks about a record of this kind, for example the tasks on a lead's card.</param>
/// <param name="SubjectId">The identifier of that record; given together with the kind.</param>
/// <param name="Page">The page number, starting from 1.</param>
/// <param name="PageSize">The number of tasks per page.</param>
[RequireRoles(UserRole.Administrator, UserRole.Manager)]
public sealed record GetStaffTasksQuery(
    Guid? AssigneeId = null,
    StaffTaskStatus? Status = null,
    bool OverdueOnly = false,
    EntityKind? SubjectKind = null,
    Guid? SubjectId = null,
    int Page = 1,
    int PageSize = PageRequest.DefaultPageSize) : IQuery<PagedList<StaffTaskListItem>>, IPagedQuery;
