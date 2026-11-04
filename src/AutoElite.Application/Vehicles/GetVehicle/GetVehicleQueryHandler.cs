namespace AutoElite.Application.Vehicles.GetVehicle;

/// <summary>Reads the vehicle and hides vehicles outside the caller's scope as not found.</summary>
internal sealed class GetVehicleQueryHandler(IVehicleQueries vehicleQueries, ICurrentUser currentUser)
    : IQueryHandler<GetVehicleQuery, VehicleItem>
{
    /// <inheritdoc />
    public async Task<Result<VehicleItem>> Handle(GetVehicleQuery query, CancellationToken cancellationToken)
    {
        var vehicle = await vehicleQueries.GetAsync(query.VehicleId, cancellationToken);

        return vehicle is not null && DataScope.For(currentUser).Includes(vehicle.BranchId)
            ? vehicle
            : Result.Failure<VehicleItem>(ApplicationErrors.NotFound<Vehicle>());
    }
}
