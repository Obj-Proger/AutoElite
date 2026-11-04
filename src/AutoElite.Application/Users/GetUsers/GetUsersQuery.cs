using AutoElite.Application.Common.Pagination;

namespace AutoElite.Application.Users.GetUsers;

/// <summary>
/// Reads a page of the user list for staff (UC-01). Managers see the users of their branch and the users not tied
/// to any branch.
/// </summary>
/// <param name="Search">Text to find in the name or email address.</param>
/// <param name="Role">Only users who hold this role.</param>
/// <param name="BranchId">Only users of this branch.</param>
/// <param name="IsActive">Only active or only deactivated users.</param>
/// <param name="Page">The page number, starting from 1.</param>
/// <param name="PageSize">The number of users per page.</param>
[RequireRoles(UserRole.Administrator, UserRole.Manager)]
public sealed record GetUsersQuery(
    string? Search = null,
    UserRole? Role = null,
    Guid? BranchId = null,
    bool? IsActive = null,
    int Page = 1,
    int PageSize = PageRequest.DefaultPageSize) : IQuery<PagedList<UserListItem>>, IPagedQuery;
