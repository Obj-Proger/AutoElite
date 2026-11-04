namespace AutoElite.Application.Vehicles.GetVehicles;

/// <summary>
/// Reads the fleet sorted by plate, for example to choose a vehicle for driving slots. A driving instructor sees the
/// vehicles of their branch and the shared ones.
/// </summary>
/// <param name="Category">Only vehicles for this licence category.</param>
/// <param name="Transmission">Only vehicles with this gearbox.</param>
/// <param name="BranchId">Only vehicles of this branch.</param>
/// <param name="IsActive">Only vehicles in service or only those taken out of service.</param>
[RequireRoles(UserRole.Administrator, UserRole.DrivingInstructor)]
public sealed record GetVehiclesQuery(
    LicenseCategory? Category = null,
    Transmission? Transmission = null,
    Guid? BranchId = null,
    bool? IsActive = null) : IQuery<IReadOnlyList<VehicleItem>>;
