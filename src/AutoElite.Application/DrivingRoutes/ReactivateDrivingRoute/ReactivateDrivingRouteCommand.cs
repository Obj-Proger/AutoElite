namespace AutoElite.Application.DrivingRoutes.ReactivateDrivingRoute;

/// <summary>Starts using a route again. The author and administrators may do it.</summary>
/// <param name="RouteId">The identifier of the route.</param>
[RequireRoles(UserRole.Administrator, UserRole.DrivingInstructor)]
public sealed record ReactivateDrivingRouteCommand(Guid RouteId) : ICommand;
