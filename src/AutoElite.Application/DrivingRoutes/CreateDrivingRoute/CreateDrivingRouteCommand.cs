namespace AutoElite.Application.DrivingRoutes.CreateDrivingRoute;

/// <summary>Writes a driving route; the signed-in instructor becomes its author (FR-6.2).</summary>
/// <param name="Name">The name.</param>
/// <param name="Description">The description: the streets, manoeuvres and difficulties.</param>
/// <param name="TrainingGroundId">The ground the route starts from; empty for none.</param>
[RequireRoles(UserRole.DrivingInstructor)]
public sealed record CreateDrivingRouteCommand(string? Name, string? Description, Guid? TrainingGroundId = null) : ICommand<Guid>;
