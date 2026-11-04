namespace AutoElite.Application.Branches.GetBranch;

/// <summary>Reads the branch.</summary>
internal sealed class GetBranchQueryHandler(IBranchQueries branchQueries) : IQueryHandler<GetBranchQuery, BranchItem>
{
    /// <inheritdoc />
    public async Task<Result<BranchItem>> Handle(GetBranchQuery query, CancellationToken cancellationToken)
    {
        var branch = await branchQueries.GetAsync(query.BranchId, cancellationToken);

        return branch is null ? Result.Failure<BranchItem>(ApplicationErrors.NotFound<Branch>()) : branch;
    }
}
