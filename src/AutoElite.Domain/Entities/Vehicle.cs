using AutoElite.Domain.Enums;
using AutoElite.Domain.ValueObjects;

namespace AutoElite.Domain.Entities;

/// <summary>A training vehicle of the school's fleet.</summary>
/// <remarks>
/// Uniqueness of the licence plate is checked by the application layer and a unique database index. A vehicle that
/// is sold or out of service is deactivated rather than deleted, because past lessons refer to it.
/// </remarks>
public sealed class Vehicle : AuditableEntity
{
    /// <summary>The earliest model year accepted.</summary>
    public const int MinYear = 1950;

    /// <summary>The longest make.</summary>
    public const int MaxMakeLength = 100;

    /// <summary>The longest model.</summary>
    public const int MaxModelLength = 100;

    /// <summary>Gets the registration plate.</summary>
    public LicensePlate Plate { get; private set; } = null!;

    /// <summary>Gets the make, for example <c>Lada</c>.</summary>
    public string Make { get; private set; } = null!;

    /// <summary>Gets the model, for example <c>Vesta</c>, if recorded.</summary>
    public string? Model { get; private set; }

    /// <summary>Gets the model year.</summary>
    public int Year { get; private set; }

    /// <summary>Gets the licence category the vehicle is used to train for.</summary>
    public LicenseCategory Category { get; private set; }

    /// <summary>Gets the gearbox type.</summary>
    public Transmission Transmission { get; private set; }

    /// <summary>Gets the identifier of the branch the vehicle belongs to, or <see langword="null"/> if shared.</summary>
    public Guid? BranchId { get; private set; }

    /// <summary>Gets a value indicating whether the vehicle is in service.</summary>
    public bool IsActive { get; private set; }

    private Vehicle(Guid id, VehicleDetails details) : base(id)
    {
        Apply(details);
        IsActive = true;
    }

    /// <summary>Required by EF Core for entity materialization.</summary>
    private Vehicle()
    {
    }

    /// <summary>Adds a vehicle to the fleet.</summary>
    /// <param name="plate">The validated registration plate.</param>
    /// <param name="make">The make. Required.</param>
    /// <param name="model">The model, if known.</param>
    /// <param name="year">The model year, from <see cref="MinYear"/> to next year.</param>
    /// <param name="category">The licence category the vehicle is used for.</param>
    /// <param name="transmission">The gearbox type.</param>
    /// <param name="branchId">The branch, if any.</param>
    /// <param name="utcNow">The current UTC time, used to check the year.</param>
    /// <returns>The vehicle, or the first validation failure encountered.</returns>
    public static Result<Vehicle> Register(
        LicensePlate plate,
        string? make,
        string? model,
        int year,
        LicenseCategory category,
        Transmission transmission,
        Guid? branchId,
        DateTime utcNow)
    {
        var detailsResult = ValidateDetails(plate, make, model, year, category, transmission, branchId, utcNow);
        if (detailsResult.IsFailure)
            return Result.Failure<Vehicle>(detailsResult.Error);

        return new Vehicle(Guid.CreateVersion7(), detailsResult.Value);
    }

    /// <summary>Replaces the vehicle details. The rules are the same as in <see cref="Register"/>.</summary>
    /// <returns>A failure if the vehicle is inactive, or the first validation failure encountered.</returns>
    public Result UpdateDetails(
        LicensePlate plate,
        string? make,
        string? model,
        int year,
        LicenseCategory category,
        Transmission transmission,
        Guid? branchId,
        DateTime utcNow)
    {
        if (!IsActive)
            return Result.Failure(DomainErrors.Vehicle.CannotModifyInactive);

        var detailsResult = ValidateDetails(plate, make, model, year, category, transmission, branchId, utcNow);
        if (detailsResult.IsFailure)
            return Result.Failure(detailsResult.Error);

        Apply(detailsResult.Value);

        return Result.Success();
    }

    /// <summary>Takes the vehicle out of service.</summary>
    /// <returns>A failure if the vehicle is already inactive.</returns>
    public Result Deactivate()
    {
        if (!IsActive)
            return Result.Failure(DomainErrors.Vehicle.AlreadyInactive);

        IsActive = false;

        return Result.Success();
    }

    /// <summary>Returns the vehicle to service.</summary>
    /// <returns>A failure if the vehicle is already active.</returns>
    public Result Reactivate()
    {
        if (IsActive)
            return Result.Failure(DomainErrors.Vehicle.AlreadyActive);

        IsActive = true;

        return Result.Success();
    }

    private void Apply(VehicleDetails details)
    {
        Plate = details.Plate;
        Make = details.Make;
        Model = details.Model;
        Year = details.Year;
        Category = details.Category;
        Transmission = details.Transmission;
        BranchId = details.BranchId;
    }

    private static Result<VehicleDetails> ValidateDetails(
        LicensePlate plate,
        string? make,
        string? model,
        int year,
        LicenseCategory category,
        Transmission transmission,
        Guid? branchId,
        DateTime utcNow)
    {
        var makeResult = Guard.RequiredText(make, MaxMakeLength, DomainErrors.Vehicle.MakeEmpty, DomainErrors.Vehicle.MakeTooLong);
        if (makeResult.IsFailure)
            return Result.Failure<VehicleDetails>(makeResult.Error);

        var modelResult = Guard.OptionalText(model, MaxModelLength, DomainErrors.Vehicle.ModelTooLong);
        if (modelResult.IsFailure)
            return Result.Failure<VehicleDetails>(modelResult.Error);

        if (year < MinYear || year > utcNow.Year + 1)
            return Result.Failure<VehicleDetails>(DomainErrors.Vehicle.YearOutOfRange);

        var categoryResult = Guard.DefinedEnum(category, DomainErrors.Vehicle.CategoryNotDefined);
        if (categoryResult.IsFailure)
            return Result.Failure<VehicleDetails>(categoryResult.Error);

        var transmissionResult = Guard.DefinedEnum(transmission, DomainErrors.Vehicle.TransmissionNotDefined);
        if (transmissionResult.IsFailure)
            return Result.Failure<VehicleDetails>(transmissionResult.Error);

        if (branchId is { } branch && branch == Guid.Empty)
            return Result.Failure<VehicleDetails>(DomainErrors.Vehicle.BranchIdEmpty);

        return new VehicleDetails(plate, makeResult.Value, modelResult.Value, year, category, transmission, branchId);
    }

    /// <summary>Validated vehicle details, passed between validation and assignment.</summary>
    private sealed record VehicleDetails(
        LicensePlate Plate, string Make, string? Model, int Year, LicenseCategory Category, Transmission Transmission, Guid? BranchId);
}
