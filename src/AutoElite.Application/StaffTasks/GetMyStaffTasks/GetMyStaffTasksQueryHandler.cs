using AutoElite.Application.Common.Pagination;

namespace AutoElite.Application.StaffTasks.GetMyStaffTasks;

/// <summary>Reads the page of the caller's tasks.</summary>
internal sealed class GetMyStaffTasksQueryHandler(IStaffTaskQueries taskQueries, ICurrentUser currentUser, TimeProvider timeProvider)
    : IQueryHandler<GetMyStaffTasksQuery, PagedList<StaffTaskListItem>>
{
    /// <inheritdoc />
    public async Task<Result<PagedList<StaffTaskListItem>>> Handle(GetMyStaffTasksQuery query, CancellationToken cancellationToken)
    {
        var filter = new StaffTaskListFilter(
            currentUser.GetRequiredUserId(),
            query.Status,
            query.OverdueOnly ? timeProvider.GetUtcNow().UtcDateTime : null,
            null);

        return await taskQueries.GetListAsync(filter, DataScope.SchoolWide, PageRequest.From(query), cancellationToken);
    }
}
