namespace AutoElite.Application.DrivingRoutes.UpdateDrivingRoute;

/// <summary>Changes a route in use. The author and administrators may change it.</summary>
/// <param name="RouteId">The identifier of the route.</param>
/// <param name="Name">The name.</param>
/// <param name="Description">The description: the streets, manoeuvres and difficulties.</param>
/// <param name="TrainingGroundId">The ground the route starts from; empty for none.</param>
[RequireRoles(UserRole.Administrator, UserRole.DrivingInstructor)]
public sealed record UpdateDrivingRouteCommand(
    Guid RouteId,
    string? Name,
    string? Description,
    Guid? TrainingGroundId = null) : ICommand;
