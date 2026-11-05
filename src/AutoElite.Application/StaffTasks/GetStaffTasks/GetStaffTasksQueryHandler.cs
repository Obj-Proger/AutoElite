using AutoElite.Application.Common.Pagination;

namespace AutoElite.Application.StaffTasks.GetStaffTasks;

/// <summary>Reads the page within the caller's scope.</summary>
internal sealed class GetStaffTasksQueryHandler(IStaffTaskQueries taskQueries, ICurrentUser currentUser, TimeProvider timeProvider)
    : IQueryHandler<GetStaffTasksQuery, PagedList<StaffTaskListItem>>
{
    /// <inheritdoc />
    public async Task<Result<PagedList<StaffTaskListItem>>> Handle(GetStaffTasksQuery query, CancellationToken cancellationToken)
    {
        var subject = query is { SubjectKind: { } kind, SubjectId: { } id } ? EntityReference.Create(kind, id).Value : null;
        var filter = new StaffTaskListFilter(
            query.AssigneeId,
            query.Status,
            query.OverdueOnly ? timeProvider.GetUtcNow().UtcDateTime : null,
            subject);

        return await taskQueries.GetListAsync(filter, DataScope.For(currentUser), PageRequest.From(query), cancellationToken);
    }
}
