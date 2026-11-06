using AutoElite.Application.Common.Authorization;
using AutoElite.Application.DrivingRoutes;
using AutoElite.Application.Vehicles;
using AutoElite.Domain.Enums;
using AutoElite.Infrastructure.Persistence.Queries;
using AutoElite.Infrastructure.Persistence.Repositories;

namespace AutoElite.Infrastructure.Tests.Persistence;

public sealed class DrivingResourcePersistenceTests(DatabaseFixture database) : IClassFixture<DatabaseFixture>
{
    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    /// <summary>A plate of its own for each vehicle: two letters and digits from a new identifier.</summary>
    private static LicensePlate NewPlate() =>
        LicensePlate.Create("AB" + string.Concat(Guid.NewGuid().ToString("N").Where(char.IsDigit).Take(8))).Value;

    private static Vehicle NewVehicle(
        LicensePlate plate, Guid? branchId = null, Transmission transmission = Transmission.Manual) =>
        Vehicle.Register(plate, "Lada", "Vesta", 2023, LicenseCategory.B, transmission, branchId, FixedClock.Default.UtcNow)
            .Value;

    private static TrainingGround NewGround(string name, Guid? branchId) =>
        TrainingGround.Create(name, Address.Create("Екатеринбург", "ул. Автодромная, 5").Value, "Площадка", branchId).Value;

    private async Task SaveAsync(params object[] records)
    {
        await using var context = database.CreateContext();
        context.AddRange(records);
        await new UnitOfWork(context).SaveChangesAsync(CancellationToken);
    }

    [Fact]
    public async Task Save_Vehicle_StoresTheNormalizedPlateWhichIsUnique()
    {
        var branch = TrainingRecords.NewBranch();
        var plate = NewPlate();
        var vehicle = NewVehicle(plate, branch.Id, Transmission.Automatic);
        await SaveAsync(branch, vehicle);

        await using (var context = database.CreateContext())
        {
            var vehicles = new VehicleRepository(context);
            var loaded = await vehicles.GetByIdAsync(vehicle.Id, CancellationToken);
            loaded!.Plate.Should().Be(plate);
            loaded.Transmission.Should().Be(Transmission.Automatic);
            (await vehicles.PlateExistsAsync(plate, null, CancellationToken)).Should().BeTrue();
            (await vehicles.PlateExistsAsync(plate, vehicle.Id, CancellationToken)).Should().BeFalse();
            (await new VehicleQueries(context).GetAsync(vehicle.Id, CancellationToken))!.BranchName.Should().Be(branch.Name);
        }

        var save = () => SaveAsync(NewVehicle(plate));

        await save.Should().ThrowAsync<ConcurrencyConflictException>();
    }

    [Fact]
    public async Task VehicleList_FiltersByScopeAndTransmissionAndSortsByPlate()
    {
        var branch = TrainingRecords.NewBranch();
        var otherBranch = TrainingRecords.NewBranch();
        var shared = NewVehicle(NewPlate(), transmission: Transmission.Automatic);
        var ofBranch = NewVehicle(NewPlate(), branch.Id);
        var ofOtherBranch = NewVehicle(NewPlate(), otherBranch.Id);
        await SaveAsync(branch, otherBranch, shared, ofBranch, ofOtherBranch);
        Guid[] mine = [shared.Id, ofBranch.Id, ofOtherBranch.Id];
        await using var context = database.CreateContext();
        var queries = new VehicleQueries(context);

        var scoped = await queries.GetListAsync(
            new VehicleListFilter(null, null, null, null), DataScope.ForBranch(branch.Id), CancellationToken);
        var automatic = await queries.GetListAsync(
            new VehicleListFilter(null, Transmission.Automatic, null, null), DataScope.SchoolWide, CancellationToken);

        scoped.Select(item => item.Id).Where(id => mine.Contains(id))
            .Should().BeEquivalentTo(new[] { shared.Id, ofBranch.Id });
        scoped.Select(item => item.Plate).Should().BeInAscendingOrder(StringComparer.Ordinal);
        automatic.Should().Contain(item => item.Id == shared.Id).And.NotContain(item => item.Id == ofBranch.Id);
    }

    [Fact]
    public async Task Grounds_AreReadWithTheirAddressAndBranch()
    {
        var branch = TrainingRecords.NewBranch();
        var ground = NewGround($"Автодром {Guid.NewGuid():N}", branch.Id);
        await SaveAsync(branch, ground);
        await using var context = database.CreateContext();
        var queries = new TrainingGroundQueries(context);

        var item = await queries.GetAsync(ground.Id, CancellationToken);
        var ofBranch = await queries.GetListAsync(branch.Id, true, DataScope.SchoolWide, CancellationToken);

        item!.City.Should().Be("Екатеринбург");
        item.Street.Should().Be("ул. Автодромная, 5");
        item.BranchName.Should().Be(branch.Name);
        ofBranch.Select(found => found.Id).Should().Equal(ground.Id);
    }

    [Fact]
    public async Task Routes_TakeTheBranchOfTheirGroundAndShowSharedOnesInEveryScope()
    {
        var author = TrainingRecords.Person("Маршрутов");
        var branch = TrainingRecords.NewBranch();
        var otherBranch = TrainingRecords.NewBranch();
        var ground = NewGround("Площадка филиала", branch.Id);
        var otherGround = NewGround("Площадка другого филиала", otherBranch.Id);
        var sharedGround = NewGround("Общая площадка", null);
        var onGround = DrivingRoute.Create("Город", "Центр и кольцо", author.Id, ground.Id).Value;
        var onOtherGround = DrivingRoute.Create("Трасса", "Объездная", author.Id, otherGround.Id).Value;
        var onSharedGround = DrivingRoute.Create("Парковка", "Задний ход", author.Id, sharedGround.Id).Value;
        var withoutGround = DrivingRoute.Create("Экзамен", "Маршрут ГИБДД", author.Id, null).Value;
        await SaveAsync(
            author, branch, otherBranch, ground, otherGround, sharedGround, onGround, onOtherGround, onSharedGround,
            withoutGround);
        await using var context = database.CreateContext();
        var queries = new DrivingRouteQueries(context);

        var scoped = await queries.GetListAsync(
            new DrivingRouteListFilter(author.Id, null, null), DataScope.ForBranch(branch.Id), CancellationToken);
        var item = await queries.GetAsync(onGround.Id, CancellationToken);

        scoped.Select(route => route.Name).Should().Equal("Город", "Парковка", "Экзамен");
        item!.AuthorName.Should().Be("Маршрутов Иван Петрович");
        item.TrainingGroundName.Should().Be("Площадка филиала");
        item.BranchId.Should().Be(branch.Id);
        (await queries.GetAsync(Guid.CreateVersion7(), CancellationToken)).Should().BeNull();
    }
}
