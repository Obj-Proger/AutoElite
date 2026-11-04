using AutoElite.Application.Common.Authorization;
using AutoElite.Application.DrivingRoutes;
using AutoElite.Application.DrivingRoutes.CreateDrivingRoute;
using AutoElite.Application.DrivingRoutes.DeactivateDrivingRoute;
using AutoElite.Application.DrivingRoutes.GetDrivingRoute;
using AutoElite.Application.DrivingRoutes.GetDrivingRoutes;
using AutoElite.Application.DrivingRoutes.ReactivateDrivingRoute;
using AutoElite.Application.DrivingRoutes.UpdateDrivingRoute;
using AutoElite.Domain.Entities;
using AutoElite.Domain.Enums;

namespace AutoElite.Application.Tests.DrivingRoutes;

public class DrivingRouteHandlerTests
{
    private readonly InMemoryRepository<DrivingRoute> _routes = new();
    private readonly InMemoryRepository<TrainingGround> _grounds = new();
    private readonly FakeDrivingRouteQueries _queries = new();
    private readonly FakeCurrentUser _author = Instructor(null);

    private static FakeCurrentUser Instructor(Guid? branchId) => new()
    {
        UserId = Guid.CreateVersion7(),
        BranchId = branchId,
        Roles = new HashSet<UserRole> { UserRole.DrivingInstructor }
    };

    private Task<Result<Guid>> Create(Guid? groundId = null, FakeCurrentUser? caller = null) =>
        new CreateDrivingRouteCommandHandler(_routes, _grounds, caller ?? _author).Handle(
            new CreateDrivingRouteCommand(" Ring road ", "Roundabouts and lane changes", groundId), TestContext.Current.CancellationToken);

    private Task<Result> Update(Guid routeId, FakeCurrentUser caller, Guid? groundId = null) =>
        new UpdateDrivingRouteCommandHandler(new DrivingRouteAccess(_routes, caller), _grounds, caller).Handle(
            new UpdateDrivingRouteCommand(routeId, "Centre", "Narrow streets and parking", groundId),
            TestContext.Current.CancellationToken);

    private Task<Result> Deactivate(Guid routeId, FakeCurrentUser caller) =>
        new DeactivateDrivingRouteCommandHandler(new DrivingRouteAccess(_routes, caller))
            .Handle(new DeactivateDrivingRouteCommand(routeId), TestContext.Current.CancellationToken);

    private DrivingRoute SeedRoute(Guid? groundId = null)
    {
        var route = DrivingRoute.Create("Ring road", "Roundabouts", _author.UserId!.Value, groundId).Value;
        _routes.Seed(route);

        return route;
    }

    private TrainingGround SeedGround(Guid? branchId = null, bool isActive = true)
    {
        var ground = TrainingGround.Create("Autodrome", Address.Create("Yekaterinburg", "Okruzhnaya 1").Value, null, branchId).Value;
        if (!isActive)
            ground.Deactivate();

        _grounds.Seed(ground);

        return ground;
    }

    private DrivingRouteItem SeedItem(Guid? branchId = null)
    {
        var item = new DrivingRouteItem(
            Guid.CreateVersion7(), "Ring road", "Roundabouts", Guid.CreateVersion7(), "Sidorov Sergey", null, null, branchId, true);
        _queries.Seed(item);

        return item;
    }

    [Fact]
    public async Task Create_FromAGroundInUse_AddsTheRouteWrittenByTheCaller()
    {
        var ground = SeedGround();

        var result = await Create(ground.Id);

        var route = _routes.Items.Should().ContainSingle().Which;
        route.Id.Should().Be(result.Value);
        route.Name.Should().Be("Ring road");
        route.AuthorId.Should().Be(_author.UserId!.Value);
        route.TrainingGroundId.Should().Be(ground.Id);
    }

    [Fact]
    public async Task Create_WithoutAGround_Succeeds()
    {
        var result = await Create();

        result.IsSuccess.Should().BeTrue();
        _routes.Items.Should().ContainSingle().Which.TrainingGroundId.Should().BeNull();
    }

    [Fact]
    public async Task Create_GroundNoLongerUsed_ReturnsTrainingGroundInactive()
    {
        var ground = SeedGround(isActive: false);

        var result = await Create(ground.Id);

        result.Error.Should().Be(ApplicationErrors.DrivingRoute.TrainingGroundInactive);
        _routes.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task Create_GroundOfAnotherBranch_ReturnsTrainingGroundNotFound()
    {
        var ground = SeedGround(Guid.CreateVersion7());

        var result = await Create(ground.Id, Instructor(Guid.CreateVersion7()));

        result.Error.Should().Be(ApplicationErrors.NotFound<TrainingGround>());
    }

    [Fact]
    public async Task Update_ByTheAuthor_AppliesTheDetails()
    {
        var route = SeedRoute();
        var ground = SeedGround();

        var result = await Update(route.Id, _author, ground.Id);

        result.IsSuccess.Should().BeTrue();
        route.Name.Should().Be("Centre");
        route.TrainingGroundId.Should().Be(ground.Id);
    }

    [Fact]
    public async Task Update_ByAnAdministrator_AppliesTheDetails()
    {
        var route = SeedRoute();

        var result = await Update(route.Id, TestData.Administrator());

        result.IsSuccess.Should().BeTrue();
        route.Description.Should().Be("Narrow streets and parking");
    }

    [Fact]
    public async Task Update_ByAnotherInstructor_ReturnsDrivingRouteNotFound()
    {
        var route = SeedRoute();

        var result = await Update(route.Id, Instructor(null));

        result.Error.Should().Be(ApplicationErrors.NotFound<DrivingRoute>());
        route.Name.Should().Be("Ring road");
    }

    [Fact]
    public async Task Update_KeepsAGroundThatIsNoLongerUsed_Succeeds()
    {
        var ground = SeedGround();
        var route = SeedRoute(ground.Id);
        ground.Deactivate();

        var result = await Update(route.Id, _author, ground.Id);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Update_MovesToAGroundNoLongerUsed_ReturnsTrainingGroundInactive()
    {
        var route = SeedRoute();
        var ground = SeedGround(isActive: false);

        var result = await Update(route.Id, _author, ground.Id);

        result.Error.Should().Be(ApplicationErrors.DrivingRoute.TrainingGroundInactive);
    }

    [Fact]
    public async Task Deactivate_ByTheAuthor_StopsUsingTheRoute()
    {
        var route = SeedRoute();

        var result = await Deactivate(route.Id, _author);

        result.IsSuccess.Should().BeTrue();
        route.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task Deactivate_ByAnotherInstructor_ReturnsDrivingRouteNotFound()
    {
        var route = SeedRoute();

        var result = await Deactivate(route.Id, Instructor(null));

        result.Error.Should().Be(ApplicationErrors.NotFound<DrivingRoute>());
        route.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task Reactivate_RouteInUse_ReturnsAlreadyActive()
    {
        var route = SeedRoute();

        var result = await new ReactivateDrivingRouteCommandHandler(new DrivingRouteAccess(_routes, _author))
            .Handle(new ReactivateDrivingRouteCommand(route.Id), TestContext.Current.CancellationToken);

        result.Error.Should().Be(DomainErrors.DrivingRoute.AlreadyActive);
    }

    [Fact]
    public async Task GetList_InstructorOfABranch_PassesTheFiltersWithTheBranchScope()
    {
        var branchId = Guid.CreateVersion7();
        var authorId = Guid.CreateVersion7();
        var groundId = Guid.CreateVersion7();

        await new GetDrivingRoutesQueryHandler(_queries, Instructor(branchId))
            .Handle(new GetDrivingRoutesQuery(authorId, groundId, true), TestContext.Current.CancellationToken);

        _queries.LastListCall.Should().Be((new DrivingRouteListFilter(authorId, groundId, true), DataScope.ForBranch(branchId)));
    }

    [Fact]
    public async Task Get_RouteWithoutABranch_IsVisibleToAnInstructorOfAnyBranch()
    {
        var item = SeedItem();

        var result = await new GetDrivingRouteQueryHandler(_queries, Instructor(Guid.CreateVersion7()))
            .Handle(new GetDrivingRouteQuery(item.Id), TestContext.Current.CancellationToken);

        result.Value.Should().Be(item);
    }

    [Fact]
    public async Task Get_RouteFromAGroundOfAnotherBranch_ReturnsDrivingRouteNotFound()
    {
        var item = SeedItem(Guid.CreateVersion7());

        var result = await new GetDrivingRouteQueryHandler(_queries, Instructor(Guid.CreateVersion7()))
            .Handle(new GetDrivingRouteQuery(item.Id), TestContext.Current.CancellationToken);

        result.Error.Should().Be(ApplicationErrors.NotFound<DrivingRoute>());
    }
}
