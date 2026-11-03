namespace AutoElite.Domain.Tests.Entities;

public class VehicleTests
{
    private static readonly DateTime _now = new(2026, 6, 1, 12, 0, 0, DateTimeKind.Utc);
    private static readonly LicensePlate _plate = LicensePlate.Create("А123ВС96").Value;
    private static readonly LicensePlate _otherPlate = LicensePlate.Create("В456ОР96").Value;
    private static readonly Guid _branchId = new("22222222-2222-2222-2222-222222222222");

    private static Vehicle RegisterVehicle() =>
        Vehicle.Register(_plate, "Lada", "Vesta", 2024, LicenseCategory.B, Transmission.Manual, null, _now).Value;

    [Fact]
    public void Register_ValidData_ReturnsActiveVehicleWithGivenValues()
    {
        var result = Vehicle.Register(_plate, "  Lada  ", "  Vesta  ", 2024, LicenseCategory.B, Transmission.Automatic, _branchId, _now);

        result.IsSuccess.Should().BeTrue();
        var vehicle = result.Value;
        vehicle.Plate.Should().Be(_plate);
        vehicle.Make.Should().Be("Lada");
        vehicle.Model.Should().Be("Vesta");
        vehicle.Year.Should().Be(2024);
        vehicle.Category.Should().Be(LicenseCategory.B);
        vehicle.Transmission.Should().Be(Transmission.Automatic);
        vehicle.BranchId.Should().Be(_branchId);
        vehicle.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Register_WithoutModel_ReturnsVehicleWithoutModel()
    {
        var result = Vehicle.Register(_plate, "Lada", "  ", 2024, LicenseCategory.B, Transmission.Manual, null, _now);

        result.Value.Model.Should().BeNull();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("   ")]
    public void Register_EmptyMake_ReturnsMakeEmptyError(string? make)
    {
        var result = Vehicle.Register(_plate, make, null, 2024, LicenseCategory.B, Transmission.Manual, null, _now);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DomainErrors.Vehicle.MakeEmpty);
    }

    [Fact]
    public void Register_MakeTooLong_ReturnsMakeTooLongError()
    {
        var result = Vehicle.Register(_plate, new string('a', 101), null, 2024, LicenseCategory.B, Transmission.Manual, null, _now);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DomainErrors.Vehicle.MakeTooLong);
    }

    [Fact]
    public void Register_ModelTooLong_ReturnsModelTooLongError()
    {
        var result = Vehicle.Register(_plate, "Lada", new string('a', 101), 2024, LicenseCategory.B, Transmission.Manual, null, _now);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DomainErrors.Vehicle.ModelTooLong);
    }

    [Theory]
    [InlineData(1950)]
    [InlineData(2027)]
    public void Register_YearAtLimit_ReturnsVehicle(int year)
    {
        var result = Vehicle.Register(_plate, "Lada", null, year, LicenseCategory.B, Transmission.Manual, null, _now);

        result.IsSuccess.Should().BeTrue();
    }

    [Theory]
    [InlineData(1949)]
    [InlineData(2028)]
    public void Register_YearOutOfRange_ReturnsYearOutOfRangeError(int year)
    {
        var result = Vehicle.Register(_plate, "Lada", null, year, LicenseCategory.B, Transmission.Manual, null, _now);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DomainErrors.Vehicle.YearOutOfRange);
    }

    [Fact]
    public void Register_UndefinedCategory_ReturnsCategoryNotDefinedError()
    {
        var result = Vehicle.Register(_plate, "Lada", null, 2024, (LicenseCategory)99, Transmission.Manual, null, _now);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DomainErrors.Vehicle.CategoryNotDefined);
    }

    [Fact]
    public void Register_UndefinedTransmission_ReturnsTransmissionNotDefinedError()
    {
        var result = Vehicle.Register(_plate, "Lada", null, 2024, LicenseCategory.B, (Transmission)0, null, _now);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DomainErrors.Vehicle.TransmissionNotDefined);
    }

    [Fact]
    public void Register_EmptyBranchId_ReturnsBranchIdEmptyError()
    {
        var result = Vehicle.Register(_plate, "Lada", null, 2024, LicenseCategory.B, Transmission.Manual, Guid.Empty, _now);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DomainErrors.Vehicle.BranchIdEmpty);
    }

    [Fact]
    public void UpdateDetails_ActiveVehicle_ReplacesDetails()
    {
        var vehicle = RegisterVehicle();

        var result = vehicle.UpdateDetails(_otherPlate, "Kia", "Rio", 2025, LicenseCategory.B, Transmission.Automatic, _branchId, _now);

        result.IsSuccess.Should().BeTrue();
        vehicle.Plate.Should().Be(_otherPlate);
        vehicle.Make.Should().Be("Kia");
        vehicle.Model.Should().Be("Rio");
        vehicle.Year.Should().Be(2025);
        vehicle.Transmission.Should().Be(Transmission.Automatic);
        vehicle.BranchId.Should().Be(_branchId);
    }

    [Fact]
    public void UpdateDetails_InvalidYear_ReturnsErrorAndKeepsDetails()
    {
        var vehicle = RegisterVehicle();

        var result = vehicle.UpdateDetails(_otherPlate, "Kia", "Rio", 1900, LicenseCategory.B, Transmission.Automatic, null, _now);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DomainErrors.Vehicle.YearOutOfRange);
        vehicle.Plate.Should().Be(_plate);
    }

    [Fact]
    public void UpdateDetails_InactiveVehicle_ReturnsCannotModifyInactiveError()
    {
        var vehicle = RegisterVehicle();
        vehicle.Deactivate();

        var result = vehicle.UpdateDetails(_otherPlate, "Kia", "Rio", 2025, LicenseCategory.B, Transmission.Automatic, null, _now);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DomainErrors.Vehicle.CannotModifyInactive);
    }

    [Fact]
    public void Deactivate_ThenReactivate_RestoresActiveState()
    {
        var vehicle = RegisterVehicle();
        vehicle.Deactivate();

        var result = vehicle.Reactivate();

        result.IsSuccess.Should().BeTrue();
        vehicle.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Deactivate_InactiveVehicle_ReturnsAlreadyInactiveError()
    {
        var vehicle = RegisterVehicle();
        vehicle.Deactivate();

        var result = vehicle.Deactivate();

        result.Error.Should().Be(DomainErrors.Vehicle.AlreadyInactive);
    }

    [Fact]
    public void Reactivate_ActiveVehicle_ReturnsAlreadyActiveError()
    {
        var vehicle = RegisterVehicle();

        var result = vehicle.Reactivate();

        result.Error.Should().Be(DomainErrors.Vehicle.AlreadyActive);
    }
}
