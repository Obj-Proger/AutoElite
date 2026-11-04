namespace AutoElite.Application.Vehicles.RegisterVehicle;

/// <summary>Checks the branch and that the plate is free, and adds the vehicle.</summary>
internal sealed class RegisterVehicleCommandHandler(
    IVehicleRepository vehicleRepository,
    IRepository<Branch> branchRepository,
    TimeProvider timeProvider) : ICommandHandler<RegisterVehicleCommand, Guid>
{
    /// <inheritdoc />
    public async Task<Result<Guid>> Handle(RegisterVehicleCommand command, CancellationToken cancellationToken)
    {
        var plateResult = LicensePlate.Create(command.Plate);
        if (plateResult.IsFailure)
            return Result.Failure<Guid>(plateResult.Error);

        var vehicleResult = Vehicle.Register(
            plateResult.Value, command.Make, command.Model, command.Year, command.Category, command.Transmission, command.BranchId,
            timeProvider.GetUtcNow().UtcDateTime);
        if (vehicleResult.IsFailure)
            return Result.Failure<Guid>(vehicleResult.Error);

        // Only administrators manage the fleet, and they see every branch.
        var branchResult = await BranchChecks.CheckAsync(branchRepository, command.BranchId, DataScope.SchoolWide, cancellationToken);
        if (branchResult.IsFailure)
            return Result.Failure<Guid>(branchResult.Error);

        if (await vehicleRepository.PlateExistsAsync(plateResult.Value, cancellationToken: cancellationToken))
            return Result.Failure<Guid>(ApplicationErrors.Vehicle.PlateTaken);

        vehicleRepository.Add(vehicleResult.Value);

        return vehicleResult.Value.Id;
    }
}
