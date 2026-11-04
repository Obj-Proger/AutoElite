namespace AutoElite.Application.Vehicles.DeactivateVehicle;

/// <summary>Takes a vehicle out of service, for example when it is sold or broken; past lessons keep referring to it.</summary>
/// <param name="VehicleId">The identifier of the vehicle.</param>
[RequireRoles(UserRole.Administrator)]
public sealed record DeactivateVehicleCommand(Guid VehicleId) : ICommand;
