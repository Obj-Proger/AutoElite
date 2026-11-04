using AutoElite.Application.Common.Authorization;
using AutoElite.Application.Vehicles;
using AutoElite.Application.Vehicles.GetVehicle;
using AutoElite.Application.Vehicles.GetVehicles;
using AutoElite.Domain.Entities;
using AutoElite.Domain.Enums;

namespace AutoElite.Application.Tests.Vehicles;

public class VehicleQueryHandlerTests
{
    private readonly FakeVehicleQueries _queries = new();

    private static FakeCurrentUser DrivingInstructor(Guid? branchId) => new()
    {
        UserId = Guid.CreateVersion7(),
        BranchId = branchId,
        Roles = new HashSet<UserRole> { UserRole.DrivingInstructor }
    };

    private VehicleItem Seed(Guid? branchId = null)
    {
        var vehicle = new VehicleItem(
            Guid.CreateVersion7(), "A123BC66", "Lada", "Vesta", 2024, LicenseCategory.B, Transmission.Manual, branchId, null, true);
        _queries.Seed(vehicle);

        return vehicle;
    }

    private Task<Result<IReadOnlyList<VehicleItem>>> GetList(GetVehiclesQuery query, FakeCurrentUser caller) =>
        new GetVehiclesQueryHandler(_queries, caller).Handle(query, TestContext.Current.CancellationToken);

    private Task<Result<VehicleItem>> Get(Guid vehicleId, FakeCurrentUser caller) =>
        new GetVehicleQueryHandler(_queries, caller).Handle(new GetVehicleQuery(vehicleId), TestContext.Current.CancellationToken);

    [Fact]
    public async Task GetList_InstructorOfABranch_PassesTheFiltersWithTheBranchScope()
    {
        var branchId = Guid.CreateVersion7();

        var result = await GetList(
            new GetVehiclesQuery(LicenseCategory.B, Transmission.Automatic, branchId, true), DrivingInstructor(branchId));

        result.IsSuccess.Should().BeTrue();
        _queries.LastListCall.Should().Be((
            new VehicleListFilter(LicenseCategory.B, Transmission.Automatic, branchId, true), DataScope.ForBranch(branchId)));
    }

    [Fact]
    public async Task GetList_Administrator_ReadsTheWholeSchool()
    {
        await GetList(new GetVehiclesQuery(), TestData.Administrator());

        _queries.LastListCall!.Value.Scope.Should().Be(DataScope.SchoolWide);
    }

    [Fact]
    public async Task GetList_InstructorAsksForAnotherBranch_ReturnsAnEmptyListWithoutReading()
    {
        Seed();

        var result = await GetList(new GetVehiclesQuery(BranchId: Guid.CreateVersion7()), DrivingInstructor(Guid.CreateVersion7()));

        result.Value.Should().BeEmpty();
        _queries.LastListCall.Should().BeNull();
    }

    [Fact]
    public async Task Get_SharedVehicle_IsVisibleToAnInstructorOfAnyBranch()
    {
        var vehicle = Seed();

        var result = await Get(vehicle.Id, DrivingInstructor(Guid.CreateVersion7()));

        result.Value.Should().Be(vehicle);
    }

    [Fact]
    public async Task Get_VehicleOfAnotherBranch_ReturnsVehicleNotFound()
    {
        var vehicle = Seed(Guid.CreateVersion7());

        var result = await Get(vehicle.Id, DrivingInstructor(Guid.CreateVersion7()));

        result.Error.Should().Be(ApplicationErrors.NotFound<Vehicle>());
    }

    [Fact]
    public async Task Get_UnknownVehicle_ReturnsVehicleNotFound()
    {
        var result = await Get(Guid.CreateVersion7(), TestData.Administrator());

        result.Error.Should().Be(ApplicationErrors.NotFound<Vehicle>());
    }

    [Fact]
    public void ListValidator_UndefinedEnumsAndEmptyBranch_ReportsEach()
    {
        var query = new GetVehiclesQuery((LicenseCategory)99, (Transmission)99, Guid.Empty);

        var codes = new GetVehiclesQueryValidator().Validate(query).Errors.Select(failure => failure.ErrorCode);

        codes.Should().Equal(
            DomainErrors.Vehicle.CategoryNotDefined.Code, DomainErrors.Vehicle.TransmissionNotDefined.Code,
            DomainErrors.Vehicle.BranchIdEmpty.Code);
    }
}
