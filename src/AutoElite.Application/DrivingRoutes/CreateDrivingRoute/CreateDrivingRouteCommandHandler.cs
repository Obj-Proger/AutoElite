using AutoElite.Application.TrainingGrounds;

namespace AutoElite.Application.DrivingRoutes.CreateDrivingRoute;

/// <summary>Checks the starting ground and adds the route written by the caller.</summary>
internal sealed class CreateDrivingRouteCommandHandler(
    IRepository<DrivingRoute> routeRepository,
    IRepository<TrainingGround> groundRepository,
    ICurrentUser currentUser) : ICommandHandler<CreateDrivingRouteCommand, Guid>
{
    /// <inheritdoc />
    public async Task<Result<Guid>> Handle(CreateDrivingRouteCommand command, CancellationToken cancellationToken)
    {
        var routeResult = DrivingRoute.Create(command.Name, command.Description, currentUser.GetRequiredUserId(), command.TrainingGroundId);
        if (routeResult.IsFailure)
            return Result.Failure<Guid>(routeResult.Error);

        var groundResult = await TrainingGroundChecks.LoadAsync(
            groundRepository, command.TrainingGroundId, DataScope.For(currentUser), ApplicationErrors.DrivingRoute.TrainingGroundInactive,
            cancellationToken);
        if (groundResult.IsFailure)
            return Result.Failure<Guid>(groundResult.Error);

        routeRepository.Add(routeResult.Value);

        return routeResult.Value.Id;
    }
}
