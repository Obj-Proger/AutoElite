using AutoElite.Application.Vehicles.DeactivateVehicle;
using AutoElite.Application.Vehicles.ReactivateVehicle;
using AutoElite.Application.Vehicles.RegisterVehicle;
using AutoElite.Application.Vehicles.UpdateVehicle;
using AutoElite.Domain.Entities;
using AutoElite.Domain.Enums;

namespace AutoElite.Application.Tests.Vehicles;

public class VehicleCommandHandlerTests
{
    private readonly FakeVehicleRepository _vehicles = new();
    private readonly InMemoryRepository<Branch> _branches = new();

    private Task<Result<Guid>> Register(string plate, Guid? branchId = null, int year = 2024) =>
        new RegisterVehicleCommandHandler(_vehicles, _branches, TestData.Clock()).Handle(
            new RegisterVehicleCommand(plate, " Lada ", "Vesta", year, LicenseCategory.B, Transmission.Manual, branchId),
            TestContext.Current.CancellationToken);

    private Task<Result> Update(Guid vehicleId, string plate, Guid? branchId = null) =>
        new UpdateVehicleCommandHandler(_vehicles, _branches, TestData.Clock()).Handle(
            new UpdateVehicleCommand(vehicleId, plate, "Kia", null, 2025, LicenseCategory.B, Transmission.Automatic, branchId),
            TestContext.Current.CancellationToken);

    private Vehicle Seed(string plate, Guid? branchId = null)
    {
        var vehicle = Vehicle.Register(
                LicensePlate.Create(plate).Value, "Lada", "Granta", 2022, LicenseCategory.B, Transmission.Manual, branchId, TestData.Now)
            .Value;
        _vehicles.Seed(vehicle);

        return vehicle;
    }

    private Branch SeedBranch(bool isActive = true)
    {
        var branch = TestData.NewBranch($"Branch {_branches.Items.Count}");
        if (!isActive)
            branch.Deactivate();

        _branches.Seed(branch);

        return branch;
    }

    [Fact]
    public async Task Register_FreePlate_AddsTheVehicleInService()
    {
        var branch = SeedBranch();

        var result = await Register("a 123-bc 66", branch.Id);

        var vehicle = _vehicles.Items.Should().ContainSingle().Which;
        vehicle.Id.Should().Be(result.Value);
        vehicle.Plate.Value.Should().Be("A123BC66");
        vehicle.Make.Should().Be("Lada");
        vehicle.BranchId.Should().Be(branch.Id);
        vehicle.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task Register_PlateOfAnotherVehicleWrittenDifferently_ReturnsPlateTaken()
    {
        Seed("A123BC66");

        var result = await Register("a 123 bc 66");

        result.Error.Should().Be(ApplicationErrors.Vehicle.PlateTaken);
        _vehicles.Items.Should().HaveCount(1);
    }

    [Fact]
    public async Task Register_PlateOfAnInactiveVehicle_ReturnsPlateTaken()
    {
        Seed("A123BC66").Deactivate();

        var result = await Register("A123BC66");

        result.Error.Should().Be(ApplicationErrors.Vehicle.PlateTaken);
    }

    [Fact]
    public async Task Register_ClosedBranch_ReturnsBranchInactive()
    {
        var branch = SeedBranch(isActive: false);

        var result = await Register("A123BC66", branch.Id);

        result.Error.Should().Be(ApplicationErrors.Branch.Inactive);
        _vehicles.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task Register_UnknownBranch_ReturnsBranchNotFound()
    {
        var result = await Register("A123BC66", Guid.CreateVersion7());

        result.Error.Should().Be(ApplicationErrors.NotFound<Branch>());
    }

    [Fact]
    public async Task Register_YearAfterNextYear_ReturnsYearOutOfRange()
    {
        var result = await Register("A123BC66", year: TestData.Now.Year + 2);

        result.Error.Should().Be(DomainErrors.Vehicle.YearOutOfRange);
    }

    [Fact]
    public async Task Update_NewDetails_AppliesThem()
    {
        var vehicle = Seed("A123BC66");

        var result = await Update(vehicle.Id, "B456CD96");

        result.IsSuccess.Should().BeTrue();
        vehicle.Plate.Value.Should().Be("B456CD96");
        vehicle.Make.Should().Be("Kia");
        vehicle.Model.Should().BeNull();
        vehicle.Transmission.Should().Be(Transmission.Automatic);
    }

    [Fact]
    public async Task Update_OwnPlate_Succeeds()
    {
        var vehicle = Seed("A123BC66");

        var result = await Update(vehicle.Id, "A123BC66");

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Update_PlateOfAnotherVehicle_ReturnsPlateTaken()
    {
        Seed("B456CD96");
        var vehicle = Seed("A123BC66");

        var result = await Update(vehicle.Id, "B456CD96");

        result.Error.Should().Be(ApplicationErrors.Vehicle.PlateTaken);
    }

    [Fact]
    public async Task Update_StaysInABranchThatHasClosed_Succeeds()
    {
        var branch = SeedBranch();
        var vehicle = Seed("A123BC66", branch.Id);
        branch.Deactivate();

        var result = await Update(vehicle.Id, "A123BC66", branch.Id);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Update_MovesToAClosedBranch_ReturnsBranchInactive()
    {
        var vehicle = Seed("A123BC66");
        var branch = SeedBranch(isActive: false);

        var result = await Update(vehicle.Id, "A123BC66", branch.Id);

        result.Error.Should().Be(ApplicationErrors.Branch.Inactive);
    }

    [Fact]
    public async Task Update_UnknownVehicle_ReturnsVehicleNotFound()
    {
        var result = await Update(Guid.CreateVersion7(), "A123BC66");

        result.Error.Should().Be(ApplicationErrors.NotFound<Vehicle>());
    }

    [Fact]
    public async Task Deactivate_VehicleInService_TakesItOutOfService()
    {
        var vehicle = Seed("A123BC66");

        var result = await new DeactivateVehicleCommandHandler(_vehicles)
            .Handle(new DeactivateVehicleCommand(vehicle.Id), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        vehicle.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task Reactivate_VehicleInService_ReturnsAlreadyActive()
    {
        var vehicle = Seed("A123BC66");

        var result = await new ReactivateVehicleCommandHandler(_vehicles)
            .Handle(new ReactivateVehicleCommand(vehicle.Id), TestContext.Current.CancellationToken);

        result.Error.Should().Be(DomainErrors.Vehicle.AlreadyActive);
    }

    [Fact]
    public void RegisterValidator_BadPlateCategoryAndTransmission_ReportsEach()
    {
        var command = new RegisterVehicleCommand("A-1!", "Lada", null, 2024, (LicenseCategory)99, (Transmission)99);

        var codes = new RegisterVehicleCommandValidator().Validate(command).Errors.Select(failure => failure.ErrorCode);

        codes.Should().Equal(
            DomainErrors.LicensePlate.InvalidFormat.Code, DomainErrors.Vehicle.CategoryNotDefined.Code,
            DomainErrors.Vehicle.TransmissionNotDefined.Code);
    }
}
