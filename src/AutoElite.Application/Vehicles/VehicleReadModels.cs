namespace AutoElite.Application.Vehicles;

/// <summary>The conditions of the fleet list; a condition that is <see langword="null"/> does not filter.</summary>
/// <param name="Category">Only vehicles for this licence category.</param>
/// <param name="Transmission">Only vehicles with this gearbox.</param>
/// <param name="BranchId">Only vehicles of this branch.</param>
/// <param name="IsActive">Only vehicles in service or only those taken out of service.</param>
public sealed record VehicleListFilter(LicenseCategory? Category, Transmission? Transmission, Guid? BranchId, bool? IsActive);

/// <summary>A vehicle of the fleet.</summary>
/// <param name="Id">The identifier of the vehicle.</param>
/// <param name="Plate">The normalized licence plate.</param>
/// <param name="Make">The make.</param>
/// <param name="Model">The model, if recorded.</param>
/// <param name="Year">The model year.</param>
/// <param name="Category">The licence category the vehicle is used for.</param>
/// <param name="Transmission">The gearbox type.</param>
/// <param name="BranchId">The branch, or <see langword="null"/> if the vehicle is shared.</param>
/// <param name="BranchName">The name of the branch, if any.</param>
/// <param name="IsActive">Whether the vehicle is in service.</param>
public sealed record VehicleItem(
    Guid Id,
    string Plate,
    string Make,
    string? Model,
    int Year,
    LicenseCategory Category,
    Transmission Transmission,
    Guid? BranchId,
    string? BranchName,
    bool IsActive);
