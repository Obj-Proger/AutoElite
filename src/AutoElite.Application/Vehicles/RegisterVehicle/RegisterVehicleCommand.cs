namespace AutoElite.Application.Vehicles.RegisterVehicle;

/// <summary>Adds a vehicle to the fleet (FR-6.1).</summary>
/// <param name="Plate">The registration plate, unique within the school; spaces and hyphens are ignored.</param>
/// <param name="Make">The make, for example <c>Lada</c>.</param>
/// <param name="Model">The model, if known.</param>
/// <param name="Year">The model year, from 1950 to next year.</param>
/// <param name="Category">The licence category the vehicle is used to train for.</param>
/// <param name="Transmission">The gearbox type.</param>
/// <param name="BranchId">The branch the vehicle belongs to; empty for a vehicle shared by all branches.</param>
[RequireRoles(UserRole.Administrator)]
public sealed record RegisterVehicleCommand(
    string? Plate,
    string? Make,
    string? Model,
    int Year,
    LicenseCategory Category,
    Transmission Transmission,
    Guid? BranchId = null) : ICommand<Guid>;
