namespace AutoElite.Application.DrivingRoutes;

/// <summary>
/// Loads a route for a change. The author of the route and administrators may change it; for anyone else the route
/// is reported as not found.
/// </summary>
internal sealed class DrivingRouteAccess(IRepository<DrivingRoute> routeRepository, ICurrentUser currentUser)
{
    /// <summary>Loads the route if the caller may change it.</summary>
    /// <param name="routeId">The identifier of the route.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    public async Task<Result<DrivingRoute>> LoadAsync(Guid routeId, CancellationToken cancellationToken)
    {
        var route = await routeRepository.GetByIdAsync(routeId, cancellationToken);

        return route is not null && (route.AuthorId == currentUser.UserId || currentUser.IsInRole(UserRole.Administrator))
            ? route
            : Result.Failure<DrivingRoute>(ApplicationErrors.NotFound<DrivingRoute>());
    }
}
