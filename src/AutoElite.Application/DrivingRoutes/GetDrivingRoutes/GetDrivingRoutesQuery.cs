namespace AutoElite.Application.DrivingRoutes.GetDrivingRoutes;

/// <summary>
/// Reads the driving routes sorted by name, for example to choose one for a slot. A driving instructor sees the
/// routes of all authors that start from the grounds of their branch, from shared grounds or from no ground.
/// </summary>
/// <param name="AuthorId">Only routes of this instructor.</param>
/// <param name="TrainingGroundId">Only routes that start from this ground.</param>
/// <param name="IsActive">Only routes in use or only those no longer used.</param>
[RequireRoles(UserRole.Administrator, UserRole.DrivingInstructor)]
public sealed record GetDrivingRoutesQuery(
    Guid? AuthorId = null,
    Guid? TrainingGroundId = null,
    bool? IsActive = null) : IQuery<IReadOnlyList<DrivingRouteItem>>;
