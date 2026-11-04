namespace AutoElite.Application.Vehicles.ReactivateVehicle;

/// <summary>Reactivates the vehicle.</summary>
internal sealed class ReactivateVehicleCommandHandler(IVehicleRepository vehicleRepository) : ICommandHandler<ReactivateVehicleCommand>
{
    /// <inheritdoc />
    public async Task<Result> Handle(ReactivateVehicleCommand command, CancellationToken cancellationToken)
    {
        var vehicle = await vehicleRepository.GetByIdAsync(command.VehicleId, cancellationToken);

        return vehicle is null ? Result.Failure(ApplicationErrors.NotFound<Vehicle>()) : vehicle.Reactivate();
    }
}
