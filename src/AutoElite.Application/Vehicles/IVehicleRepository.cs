namespace AutoElite.Application.Vehicles;

/// <summary>Loads and adds vehicles for commands.</summary>
public interface IVehicleRepository : IRepository<Vehicle>
{
    /// <summary>Determines whether another vehicle, active or not, already has the licence plate.</summary>
    /// <param name="plate">The normalized licence plate.</param>
    /// <param name="exceptVehicleId">A vehicle to leave out, when the plate of that vehicle is being changed.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    Task<bool> PlateExistsAsync(LicensePlate plate, Guid? exceptVehicleId = null, CancellationToken cancellationToken = default);
}
