namespace AutoElite.Application.Vehicles;

/// <summary>Reads vehicles for queries.</summary>
public interface IVehicleQueries
{
    /// <summary>
    /// Reads the vehicles that match the filter and are inside the scope, sorted by plate. A school has a small fleet,
    /// so the list is not paged.
    /// </summary>
    /// <param name="filter">The conditions of the list.</param>
    /// <param name="scope">The branches the caller may see; shared vehicles are in every scope.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    Task<IReadOnlyList<VehicleItem>> GetListAsync(VehicleListFilter filter, DataScope scope, CancellationToken cancellationToken = default);

    /// <summary>Reads one vehicle.</summary>
    /// <param name="vehicleId">The identifier of the vehicle.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    /// <returns>The vehicle, or <see langword="null"/> if it does not exist.</returns>
    Task<VehicleItem?> GetAsync(Guid vehicleId, CancellationToken cancellationToken = default);
}
