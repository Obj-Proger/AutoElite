namespace AutoElite.Application.Vehicles.UpdateVehicle;

/// <summary>Checks the branch and that the plate is free among the other vehicles, and applies the details.</summary>
internal sealed class UpdateVehicleCommandHandler(
    IVehicleRepository vehicleRepository,
    IRepository<Branch> branchRepository,
    TimeProvider timeProvider) : ICommandHandler<UpdateVehicleCommand>
{
    /// <inheritdoc />
    public async Task<Result> Handle(UpdateVehicleCommand command, CancellationToken cancellationToken)
    {
        var vehicle = await vehicleRepository.GetByIdAsync(command.VehicleId, cancellationToken);
        if (vehicle is null)
            return Result.Failure(ApplicationErrors.NotFound<Vehicle>());

        var plateResult = LicensePlate.Create(command.Plate);
        if (plateResult.IsFailure)
            return Result.Failure(plateResult.Error);

        var previousBranchId = vehicle.BranchId;
        var updateResult = vehicle.UpdateDetails(
            plateResult.Value, command.Make, command.Model, command.Year, command.Category, command.Transmission, command.BranchId,
            timeProvider.GetUtcNow().UtcDateTime);
        if (updateResult.IsFailure)
            return updateResult;

        // A vehicle can stay in a branch that has since closed; only a move is checked. Only administrators manage
        // the fleet, and they see every branch.
        if (vehicle.BranchId != previousBranchId)
        {
            var branchResult = await BranchChecks.CheckAsync(branchRepository, vehicle.BranchId, DataScope.SchoolWide, cancellationToken);
            if (branchResult.IsFailure)
                return branchResult;
        }

        return await vehicleRepository.PlateExistsAsync(vehicle.Plate, vehicle.Id, cancellationToken)
            ? Result.Failure(ApplicationErrors.Vehicle.PlateTaken)
            : Result.Success();
    }
}
