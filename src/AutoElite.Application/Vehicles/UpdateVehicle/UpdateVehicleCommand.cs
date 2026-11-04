namespace AutoElite.Application.Vehicles.UpdateVehicle;

/// <summary>Changes the details of a vehicle in service.</summary>
/// <param name="VehicleId">The identifier of the vehicle.</param>
/// <param name="Plate">The registration plate, unique within the school.</param>
/// <param name="Make">The make.</param>
/// <param name="Model">The model; empty removes it.</param>
/// <param name="Year">The model year, from 1950 to next year.</param>
/// <param name="Category">The licence category the vehicle is used to train for.</param>
/// <param name="Transmission">The gearbox type.</param>
/// <param name="BranchId">The branch the vehicle belongs to; empty for a vehicle shared by all branches.</param>
[RequireRoles(UserRole.Administrator)]
public sealed record UpdateVehicleCommand(
    Guid VehicleId,
    string? Plate,
    string? Make,
    string? Model,
    int Year,
    LicenseCategory Category,
    Transmission Transmission,
    Guid? BranchId = null) : ICommand;
