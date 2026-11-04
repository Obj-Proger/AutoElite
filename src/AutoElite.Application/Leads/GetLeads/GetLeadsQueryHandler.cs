using AutoElite.Application.Common.Pagination;

namespace AutoElite.Application.Leads.GetLeads;

/// <summary>Reads the page within the caller's scope; a branch outside the scope gives an empty page.</summary>
internal sealed class GetLeadsQueryHandler(ILeadQueries leadQueries, ICurrentUser currentUser)
    : IQueryHandler<GetLeadsQuery, PagedList<LeadListItem>>
{
    /// <inheritdoc />
    public async Task<Result<PagedList<LeadListItem>>> Handle(GetLeadsQuery query, CancellationToken cancellationToken)
    {
        var page = PageRequest.From(query);
        var scope = DataScope.For(currentUser);

        if (query.BranchId is { } branchId && !scope.Includes(branchId))
            return PagedList<LeadListItem>.Empty(page);

        var search = string.IsNullOrWhiteSpace(query.Search) ? null : query.Search.Trim();
        var filter = new LeadListFilter(search, query.Status, query.Source, query.ManagerId, query.BranchId);

        return await leadQueries.GetListAsync(filter, scope, page, cancellationToken);
    }
}
