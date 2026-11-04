using AutoElite.Application.Common.Pagination;

namespace AutoElite.Application.StudyGroups.GetStudyGroups;

/// <summary>
/// Reads a page of the group list, the latest start first. Managers see the groups of their branch and those without
/// a branch; a theory instructor sees only their own groups.
/// </summary>
/// <param name="Search">Text to find in the group name.</param>
/// <param name="CourseId">Only groups of this course.</param>
/// <param name="TheoryInstructorId">Only groups of this instructor.</param>
/// <param name="BranchId">Only groups of this branch.</param>
/// <param name="IsActive">Only active or only closed groups.</param>
/// <param name="Page">The page number, starting from 1.</param>
/// <param name="PageSize">The number of groups per page.</param>
[RequireRoles(UserRole.Administrator, UserRole.Manager, UserRole.TheoryInstructor)]
public sealed record GetStudyGroupsQuery(
    string? Search = null,
    Guid? CourseId = null,
    Guid? TheoryInstructorId = null,
    Guid? BranchId = null,
    bool? IsActive = null,
    int Page = 1,
    int PageSize = PageRequest.DefaultPageSize) : IQuery<PagedList<StudyGroupListItem>>, IPagedQuery;
