namespace AutoElite.Application.DrivingRoutes.GetDrivingRoute;

/// <summary>Reads one route. A driving instructor sees the routes that start in their branch or in no branch.</summary>
/// <param name="RouteId">The identifier of the route.</param>
[RequireRoles(UserRole.Administrator, UserRole.DrivingInstructor)]
public sealed record GetDrivingRouteQuery(Guid RouteId) : IQuery<DrivingRouteItem>;
