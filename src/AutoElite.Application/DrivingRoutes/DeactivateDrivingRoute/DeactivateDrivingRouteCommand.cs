namespace AutoElite.Application.DrivingRoutes.DeactivateDrivingRoute;

/// <summary>Stops using a route; past lessons keep referring to it. The author and administrators may do it.</summary>
/// <param name="RouteId">The identifier of the route.</param>
[RequireRoles(UserRole.Administrator, UserRole.DrivingInstructor)]
public sealed record DeactivateDrivingRouteCommand(Guid RouteId) : ICommand;
