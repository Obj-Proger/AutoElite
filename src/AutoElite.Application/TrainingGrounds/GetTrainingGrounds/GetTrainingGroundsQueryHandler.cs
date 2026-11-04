namespace AutoElite.Application.TrainingGrounds.GetTrainingGrounds;

/// <summary>Reads the grounds within the caller's scope; a branch outside the scope gives an empty list.</summary>
internal sealed class GetTrainingGroundsQueryHandler(ITrainingGroundQueries groundQueries, ICurrentUser currentUser)
    : IQueryHandler<GetTrainingGroundsQuery, IReadOnlyList<TrainingGroundItem>>
{
    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<TrainingGroundItem>>> Handle(GetTrainingGroundsQuery query, CancellationToken cancellationToken)
    {
        var scope = DataScope.For(currentUser);
        if (query.BranchId is { } branchId && !scope.Includes(branchId))
            return Result.Success<IReadOnlyList<TrainingGroundItem>>([]);

        return Result.Success(await groundQueries.GetListAsync(query.BranchId, query.IsActive, scope, cancellationToken));
    }
}
