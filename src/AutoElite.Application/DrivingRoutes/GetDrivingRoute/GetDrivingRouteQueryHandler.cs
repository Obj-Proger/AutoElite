namespace AutoElite.Application.DrivingRoutes.GetDrivingRoute;

/// <summary>Reads the route and hides routes outside the caller's scope as not found.</summary>
internal sealed class GetDrivingRouteQueryHandler(IDrivingRouteQueries routeQueries, ICurrentUser currentUser)
    : IQueryHandler<GetDrivingRouteQuery, DrivingRouteItem>
{
    /// <inheritdoc />
    public async Task<Result<DrivingRouteItem>> Handle(GetDrivingRouteQuery query, CancellationToken cancellationToken)
    {
        var route = await routeQueries.GetAsync(query.RouteId, cancellationToken);

        return route is not null && DataScope.For(currentUser).Includes(route.BranchId)
            ? route
            : Result.Failure<DrivingRouteItem>(ApplicationErrors.NotFound<DrivingRoute>());
    }
}
