using AutoElite.Application.TrainingGrounds;

namespace AutoElite.Application.DrivingRoutes.UpdateDrivingRoute;

/// <summary>Applies the details and checks the starting ground when it changes.</summary>
internal sealed class UpdateDrivingRouteCommandHandler(
    DrivingRouteAccess routeAccess,
    IRepository<TrainingGround> groundRepository,
    ICurrentUser currentUser) : ICommandHandler<UpdateDrivingRouteCommand>
{
    /// <inheritdoc />
    public async Task<Result> Handle(UpdateDrivingRouteCommand command, CancellationToken cancellationToken)
    {
        var routeResult = await routeAccess.LoadAsync(command.RouteId, cancellationToken);
        if (routeResult.IsFailure)
            return routeResult;

        var route = routeResult.Value;
        var previousGroundId = route.TrainingGroundId;
        var updateResult = route.UpdateDetails(command.Name, command.Description, command.TrainingGroundId);
        if (updateResult.IsFailure || route.TrainingGroundId == previousGroundId)
            return updateResult;

        // A route can keep a ground that is no longer used; only a new starting ground is checked.
        var groundResult = await TrainingGroundChecks.LoadAsync(
            groundRepository, route.TrainingGroundId, DataScope.For(currentUser), ApplicationErrors.DrivingRoute.TrainingGroundInactive,
            cancellationToken);

        return groundResult.IsFailure ? Result.Failure(groundResult.Error) : Result.Success();
    }
}
