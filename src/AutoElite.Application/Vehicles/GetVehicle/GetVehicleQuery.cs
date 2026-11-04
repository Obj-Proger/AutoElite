namespace AutoElite.Application.Vehicles.GetVehicle;

/// <summary>Reads one vehicle. A driving instructor sees the vehicles of their branch and the shared ones.</summary>
/// <param name="VehicleId">The identifier of the vehicle.</param>
[RequireRoles(UserRole.Administrator, UserRole.DrivingInstructor)]
public sealed record GetVehicleQuery(Guid VehicleId) : IQuery<VehicleItem>;
