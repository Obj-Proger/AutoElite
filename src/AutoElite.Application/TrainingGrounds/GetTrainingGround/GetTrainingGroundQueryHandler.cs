namespace AutoElite.Application.TrainingGrounds.GetTrainingGround;

/// <summary>Reads the ground and hides grounds outside the caller's scope as not found.</summary>
internal sealed class GetTrainingGroundQueryHandler(ITrainingGroundQueries groundQueries, ICurrentUser currentUser)
    : IQueryHandler<GetTrainingGroundQuery, TrainingGroundItem>
{
    /// <inheritdoc />
    public async Task<Result<TrainingGroundItem>> Handle(GetTrainingGroundQuery query, CancellationToken cancellationToken)
    {
        var ground = await groundQueries.GetAsync(query.GroundId, cancellationToken);

        return ground is not null && DataScope.For(currentUser).Includes(ground.BranchId)
            ? ground
            : Result.Failure<TrainingGroundItem>(ApplicationErrors.NotFound<TrainingGround>());
    }
}
