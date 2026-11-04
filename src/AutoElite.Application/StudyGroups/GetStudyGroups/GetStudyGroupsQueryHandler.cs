using AutoElite.Application.Common.Pagination;

namespace AutoElite.Application.StudyGroups.GetStudyGroups;

/// <summary>
/// Reads the page within the caller's scope. A theory instructor who is not also staff of the office reads only
/// their own groups; asking for someone else's groups or a branch outside the scope gives an empty page.
/// </summary>
internal sealed class GetStudyGroupsQueryHandler(IStudyGroupQueries groupQueries, ICurrentUser currentUser)
    : IQueryHandler<GetStudyGroupsQuery, PagedList<StudyGroupListItem>>
{
    /// <inheritdoc />
    public async Task<Result<PagedList<StudyGroupListItem>>> Handle(GetStudyGroupsQuery query, CancellationToken cancellationToken)
    {
        var page = PageRequest.From(query);
        var scope = DataScope.For(currentUser);

        var instructorId = query.TheoryInstructorId;
        if (!currentUser.IsInAnyRole(UserRole.Administrator, UserRole.Manager))
        {
            var ownId = currentUser.GetRequiredUserId();
            if (instructorId is { } requested && requested != ownId)
                return PagedList<StudyGroupListItem>.Empty(page);

            instructorId = ownId;
        }

        if (query.BranchId is { } branchId && !scope.Includes(branchId))
            return PagedList<StudyGroupListItem>.Empty(page);

        var search = string.IsNullOrWhiteSpace(query.Search) ? null : query.Search.Trim();
        var filter = new StudyGroupListFilter(search, query.CourseId, instructorId, query.BranchId, query.IsActive);

        return await groupQueries.GetListAsync(filter, scope, page, cancellationToken);
    }
}
