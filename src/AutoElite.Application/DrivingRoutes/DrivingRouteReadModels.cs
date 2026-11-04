namespace AutoElite.Application.DrivingRoutes;

/// <summary>The conditions of the route list; a condition that is <see langword="null"/> does not filter.</summary>
/// <param name="AuthorId">Only routes of this instructor.</param>
/// <param name="TrainingGroundId">Only routes that start from this ground.</param>
/// <param name="IsActive">Only routes in use or only those no longer used.</param>
public sealed record DrivingRouteListFilter(Guid? AuthorId, Guid? TrainingGroundId, bool? IsActive);

/// <summary>A driving route.</summary>
/// <param name="Id">The identifier of the route.</param>
/// <param name="Name">The name.</param>
/// <param name="Description">The description of the route: the streets, manoeuvres and difficulties.</param>
/// <param name="AuthorId">The driving instructor who wrote the route.</param>
/// <param name="AuthorName">The full name of the author.</param>
/// <param name="TrainingGroundId">The ground the route starts from, if any.</param>
/// <param name="TrainingGroundName">The name of the ground, if any.</param>
/// <param name="BranchId">The branch of the ground, if any; a route without one is visible in every branch.</param>
/// <param name="IsActive">Whether the route is in use.</param>
public sealed record DrivingRouteItem(
    Guid Id,
    string Name,
    string Description,
    Guid AuthorId,
    string AuthorName,
    Guid? TrainingGroundId,
    string? TrainingGroundName,
    Guid? BranchId,
    bool IsActive);
