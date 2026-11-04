namespace AutoElite.Application.DrivingRoutes.GetDrivingRoutes;

/// <summary>Reads the routes within the caller's scope.</summary>
internal sealed class GetDrivingRoutesQueryHandler(IDrivingRouteQueries routeQueries, ICurrentUser currentUser)
    : IQueryHandler<GetDrivingRoutesQuery, IReadOnlyList<DrivingRouteItem>>
{
    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<DrivingRouteItem>>> Handle(GetDrivingRoutesQuery query, CancellationToken cancellationToken)
    {
        var filter = new DrivingRouteListFilter(query.AuthorId, query.TrainingGroundId, query.IsActive);

        return Result.Success(await routeQueries.GetListAsync(filter, DataScope.For(currentUser), cancellationToken));
    }
}
