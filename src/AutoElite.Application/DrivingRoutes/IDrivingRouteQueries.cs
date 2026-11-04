namespace AutoElite.Application.DrivingRoutes;

/// <summary>Reads driving routes for queries.</summary>
public interface IDrivingRouteQueries
{
    /// <summary>
    /// Reads the routes that match the filter and whose ground is inside the scope, sorted by name. Routes without a
    /// ground and routes from a shared ground are in every scope. A school has few routes, so the list is not paged.
    /// </summary>
    /// <param name="filter">The conditions of the list.</param>
    /// <param name="scope">The branches the caller may see.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    Task<IReadOnlyList<DrivingRouteItem>> GetListAsync(
        DrivingRouteListFilter filter, DataScope scope, CancellationToken cancellationToken = default);

    /// <summary>Reads one route.</summary>
    /// <param name="routeId">The identifier of the route.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    /// <returns>The route, or <see langword="null"/> if it does not exist.</returns>
    Task<DrivingRouteItem?> GetAsync(Guid routeId, CancellationToken cancellationToken = default);
}
