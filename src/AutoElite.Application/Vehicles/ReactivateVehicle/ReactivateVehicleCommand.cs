namespace AutoElite.Application.Vehicles.ReactivateVehicle;

/// <summary>Returns a vehicle to service.</summary>
/// <param name="VehicleId">The identifier of the vehicle.</param>
[RequireRoles(UserRole.Administrator)]
public sealed record ReactivateVehicleCommand(Guid VehicleId) : ICommand;
