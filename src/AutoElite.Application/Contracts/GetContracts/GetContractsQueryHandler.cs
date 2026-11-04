using AutoElite.Application.Common.Pagination;

namespace AutoElite.Application.Contracts.GetContracts;

/// <summary>Reads the page within the caller's scope; a branch outside the scope gives an empty page.</summary>
internal sealed class GetContractsQueryHandler(IContractQueries contractQueries, ICurrentUser currentUser)
    : IQueryHandler<GetContractsQuery, PagedList<ContractListItem>>
{
    /// <inheritdoc />
    public async Task<Result<PagedList<ContractListItem>>> Handle(GetContractsQuery query, CancellationToken cancellationToken)
    {
        var page = PageRequest.From(query);
        var scope = DataScope.For(currentUser);

        if (query.BranchId is { } branchId && !scope.Includes(branchId))
            return PagedList<ContractListItem>.Empty(page);

        var search = string.IsNullOrWhiteSpace(query.Search) ? null : query.Search.Trim();
        var filter = new ContractListFilter(search, query.Status, query.CourseId, query.BranchId, query.StudentId, query.HasDebt);

        return await contractQueries.GetListAsync(filter, scope, page, cancellationToken);
    }
}
