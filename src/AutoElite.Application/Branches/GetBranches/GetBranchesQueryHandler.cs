namespace AutoElite.Application.Branches.GetBranches;

/// <summary>Reads the branches.</summary>
internal sealed class GetBranchesQueryHandler(IBranchQueries branchQueries)
    : IQueryHandler<GetBranchesQuery, IReadOnlyList<BranchItem>>
{
    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<BranchItem>>> Handle(GetBranchesQuery query, CancellationToken cancellationToken) =>
        Result.Success(await branchQueries.GetListAsync(query.IsActive, cancellationToken));
}
