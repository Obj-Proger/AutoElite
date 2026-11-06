using AutoElite.Application.Vehicles;

namespace AutoElite.Infrastructure.Persistence.Repositories;

/// <summary>Loads and adds vehicles.</summary>
internal sealed class VehicleRepository(AutoEliteDbContext dbContext) : Repository<Vehicle>(dbContext), IVehicleRepository
{
    /// <inheritdoc />
    public Task<bool> PlateExistsAsync(
        LicensePlate plate, Guid? exceptVehicleId = null, CancellationToken cancellationToken = default) =>
        Aggregates.AnyAsync(vehicle => vehicle.Plate == plate && vehicle.Id != exceptVehicleId, cancellationToken);
}

/// <summary>Loads and adds training grounds.</summary>
internal sealed class TrainingGroundRepository(AutoEliteDbContext dbContext) : Repository<TrainingGround>(dbContext);

/// <summary>Loads and adds driving routes.</summary>
internal sealed class DrivingRouteRepository(AutoEliteDbContext dbContext) : Repository<DrivingRoute>(dbContext);
