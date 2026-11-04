namespace AutoElite.Application.Vehicles.DeactivateVehicle;

/// <summary>Deactivates the vehicle.</summary>
internal sealed class DeactivateVehicleCommandHandler(IVehicleRepository vehicleRepository) : ICommandHandler<DeactivateVehicleCommand>
{
    /// <inheritdoc />
    public async Task<Result> Handle(DeactivateVehicleCommand command, CancellationToken cancellationToken)
    {
        var vehicle = await vehicleRepository.GetByIdAsync(command.VehicleId, cancellationToken);

        return vehicle is null ? Result.Failure(ApplicationErrors.NotFound<Vehicle>()) : vehicle.Deactivate();
    }
}
