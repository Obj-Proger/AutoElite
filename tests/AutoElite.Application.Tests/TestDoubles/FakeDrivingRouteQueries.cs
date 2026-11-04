using AutoElite.Application.Common.Authorization;
using AutoElite.Application.DrivingRoutes;

namespace AutoElite.Application.Tests.TestDoubles;

/// <summary>Returns seeded routes and records the list arguments; filtering is the infrastructure's job.</summary>
internal sealed class FakeDrivingRouteQueries : IDrivingRouteQueries
{
    private readonly Dictionary<Guid, DrivingRouteItem> _routes = [];

    public (DrivingRouteListFilter Filter, DataScope Scope)? LastListCall { get; private set; }

    public void Seed(DrivingRouteItem route) => _routes.Add(route.Id, route);

    public Task<IReadOnlyList<DrivingRouteItem>> GetListAsync(
        DrivingRouteListFilter filter, DataScope scope, CancellationToken cancellationToken = default)
    {
        LastListCall = (filter, scope);

        return Task.FromResult<IReadOnlyList<DrivingRouteItem>>([.. _routes.Values]);
    }

    public Task<DrivingRouteItem?> GetAsync(Guid routeId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_routes.GetValueOrDefault(routeId));
}
