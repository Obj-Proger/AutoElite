using AutoElite.Application.TrainingGrounds;

namespace AutoElite.Application.DrivingSlots.OfferDrivingSlot;

/// <summary>
/// Checks that the vehicle, the ground and the route are visible to the caller and that the caller and the vehicle
/// are free at that time, and offers the slot.
/// </summary>
internal sealed class OfferDrivingSlotCommandHandler(
    IDrivingSlotRepository slotRepository,
    IRepository<Vehicle> vehicleRepository,
    IRepository<TrainingGround> groundRepository,
    IRepository<DrivingRoute> routeRepository,
    ICurrentUser currentUser,
    TimeProvider timeProvider) : ICommandHandler<OfferDrivingSlotCommand, Guid>
{
    /// <inheritdoc />
    public async Task<Result<Guid>> Handle(OfferDrivingSlotCommand command, CancellationToken cancellationToken)
    {
        var scope = DataScope.For(currentUser);

        var vehicle = await vehicleRepository.GetByIdAsync(command.VehicleId, cancellationToken);
        if (vehicle is null || !scope.Includes(vehicle.BranchId))
            return Result.Failure<Guid>(ApplicationErrors.NotFound<Vehicle>());

        var groundResult = await TrainingGroundChecks.LoadAsync(
            groundRepository, command.TrainingGroundId, scope, DomainErrors.DrivingSlot.TrainingGroundInactive, cancellationToken);
        if (groundResult.IsFailure)
            return Result.Failure<Guid>(groundResult.Error);

        var routeResult = await LoadRouteAsync(command.RouteId, scope, cancellationToken);
        if (routeResult.IsFailure)
            return Result.Failure<Guid>(routeResult.Error);

        var timeResult = TimeInterval.Create(command.StartUtc, command.EndUtc);
        if (timeResult.IsFailure)
            return Result.Failure<Guid>(timeResult.Error);

        var instructorId = currentUser.GetRequiredUserId();
        var slotResult = DrivingSlot.Offer(
            instructorId, vehicle, groundResult.Value, routeResult.Value, timeResult.Value, timeProvider.GetUtcNow().UtcDateTime);
        if (slotResult.IsFailure)
            return Result.Failure<Guid>(slotResult.Error);

        if (await slotRepository.HasOverlapAsync(instructorId, vehicle.Id, timeResult.Value, cancellationToken))
            return Result.Failure<Guid>(ApplicationErrors.DrivingSlot.TimeConflict);

        slotRepository.Add(slotResult.Value);

        return slotResult.Value.Id;
    }

    // A route is visible where its starting ground is; a route without a ground is visible everywhere.
    private async Task<Result<DrivingRoute?>> LoadRouteAsync(Guid? routeId, DataScope scope, CancellationToken cancellationToken)
    {
        if (routeId is not { } id)
            return Result.Success<DrivingRoute?>(null);

        var route = await routeRepository.GetByIdAsync(id, cancellationToken);
        if (route is null)
            return Result.Failure<DrivingRoute?>(ApplicationErrors.NotFound<DrivingRoute>());

        if (route.TrainingGroundId is { } groundId)
        {
            var ground = await groundRepository.GetByIdAsync(groundId, cancellationToken);
            if (ground is not null && !scope.Includes(ground.BranchId))
                return Result.Failure<DrivingRoute?>(ApplicationErrors.NotFound<DrivingRoute>());
        }

        return Result.Success<DrivingRoute?>(route);
    }
}
