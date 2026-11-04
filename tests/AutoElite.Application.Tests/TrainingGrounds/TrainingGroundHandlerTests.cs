using AutoElite.Application.Common.Authorization;
using AutoElite.Application.TrainingGrounds;
using AutoElite.Application.TrainingGrounds.CreateTrainingGround;
using AutoElite.Application.TrainingGrounds.DeactivateTrainingGround;
using AutoElite.Application.TrainingGrounds.GetTrainingGround;
using AutoElite.Application.TrainingGrounds.GetTrainingGrounds;
using AutoElite.Application.TrainingGrounds.ReactivateTrainingGround;
using AutoElite.Application.TrainingGrounds.UpdateTrainingGround;
using AutoElite.Domain.Entities;
using AutoElite.Domain.Enums;

namespace AutoElite.Application.Tests.TrainingGrounds;

public class TrainingGroundHandlerTests
{
    private readonly InMemoryRepository<TrainingGround> _grounds = new();
    private readonly InMemoryRepository<Branch> _branches = new();
    private readonly FakeTrainingGroundQueries _queries = new();

    private static FakeCurrentUser DrivingInstructor(Guid? branchId) => new()
    {
        UserId = Guid.CreateVersion7(),
        BranchId = branchId,
        Roles = new HashSet<UserRole> { UserRole.DrivingInstructor }
    };

    private Task<Result<Guid>> Create(Guid? branchId = null, string name = " Autodrome ") =>
        new CreateTrainingGroundCommandHandler(_grounds, _branches).Handle(
            new CreateTrainingGroundCommand(name, "Yekaterinburg", "Okruzhnaya 1", "Gate 2", branchId),
            TestContext.Current.CancellationToken);

    private Task<Result> Update(Guid groundId, Guid? branchId = null) =>
        new UpdateTrainingGroundCommandHandler(_grounds, _branches).Handle(
            new UpdateTrainingGroundCommand(groundId, "City start", "Yekaterinburg", "Lenina 50", null, branchId),
            TestContext.Current.CancellationToken);

    private TrainingGround Seed(Guid? branchId = null)
    {
        var ground = TrainingGround.Create("Autodrome", Address.Create("Yekaterinburg", "Okruzhnaya 1").Value, null, branchId).Value;
        _grounds.Seed(ground);

        return ground;
    }

    private Branch SeedBranch(bool isActive = true)
    {
        var branch = TestData.NewBranch($"Branch {_branches.Items.Count}");
        if (!isActive)
            branch.Deactivate();

        _branches.Seed(branch);

        return branch;
    }

    private TrainingGroundItem SeedItem(Guid? branchId = null)
    {
        var item = new TrainingGroundItem(Guid.CreateVersion7(), "Autodrome", "Yekaterinburg", "Okruzhnaya 1", null, branchId, null, true);
        _queries.Seed(item);

        return item;
    }

    [Fact]
    public async Task Create_ValidDetails_AddsTheGroundInUse()
    {
        var branch = SeedBranch();

        var result = await Create(branch.Id);

        var ground = _grounds.Items.Should().ContainSingle().Which;
        ground.Id.Should().Be(result.Value);
        ground.Name.Should().Be("Autodrome");
        ground.Address.Should().Be(Address.Create("Yekaterinburg", "Okruzhnaya 1").Value);
        ground.Description.Should().Be("Gate 2");
        ground.BranchId.Should().Be(branch.Id);
        ground.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task Create_ClosedBranch_ReturnsBranchInactive()
    {
        var branch = SeedBranch(isActive: false);

        var result = await Create(branch.Id);

        result.Error.Should().Be(ApplicationErrors.Branch.Inactive);
        _grounds.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task Create_EmptyName_ReturnsNameEmpty()
    {
        var result = await Create(name: "  ");

        result.Error.Should().Be(DomainErrors.TrainingGround.NameEmpty);
    }

    [Fact]
    public async Task Update_NewDetails_AppliesThem()
    {
        var ground = Seed();
        var branch = SeedBranch();

        var result = await Update(ground.Id, branch.Id);

        result.IsSuccess.Should().BeTrue();
        ground.Name.Should().Be("City start");
        ground.Address.Street.Should().Be("Lenina 50");
        ground.BranchId.Should().Be(branch.Id);
    }

    [Fact]
    public async Task Update_StaysInABranchThatHasClosed_Succeeds()
    {
        var branch = SeedBranch();
        var ground = Seed(branch.Id);
        branch.Deactivate();

        var result = await Update(ground.Id, branch.Id);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Update_MovesToAClosedBranch_ReturnsBranchInactive()
    {
        var ground = Seed();
        var branch = SeedBranch(isActive: false);

        var result = await Update(ground.Id, branch.Id);

        result.Error.Should().Be(ApplicationErrors.Branch.Inactive);
    }

    [Fact]
    public async Task Update_InactiveGround_ReturnsCannotModifyInactive()
    {
        var ground = Seed();
        ground.Deactivate();

        var result = await Update(ground.Id);

        result.Error.Should().Be(DomainErrors.TrainingGround.CannotModifyInactive);
    }

    [Fact]
    public async Task Deactivate_ThenReactivate_ReturnsTheGroundToUse()
    {
        var ground = Seed();

        var deactivated = await new DeactivateTrainingGroundCommandHandler(_grounds)
            .Handle(new DeactivateTrainingGroundCommand(ground.Id), TestContext.Current.CancellationToken);
        var reactivated = await new ReactivateTrainingGroundCommandHandler(_grounds)
            .Handle(new ReactivateTrainingGroundCommand(ground.Id), TestContext.Current.CancellationToken);

        deactivated.IsSuccess.Should().BeTrue();
        reactivated.IsSuccess.Should().BeTrue();
        ground.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task Deactivate_UnknownGround_ReturnsTrainingGroundNotFound()
    {
        var result = await new DeactivateTrainingGroundCommandHandler(_grounds)
            .Handle(new DeactivateTrainingGroundCommand(Guid.CreateVersion7()), TestContext.Current.CancellationToken);

        result.Error.Should().Be(ApplicationErrors.NotFound<TrainingGround>());
    }

    [Fact]
    public async Task GetList_InstructorOfABranch_PassesTheFiltersWithTheBranchScope()
    {
        var branchId = Guid.CreateVersion7();

        await new GetTrainingGroundsQueryHandler(_queries, DrivingInstructor(branchId))
            .Handle(new GetTrainingGroundsQuery(branchId, true), TestContext.Current.CancellationToken);

        _queries.LastListCall.Should().Be(((Guid?)branchId, (bool?)true, DataScope.ForBranch(branchId)));
    }

    [Fact]
    public async Task GetList_InstructorAsksForAnotherBranch_ReturnsAnEmptyListWithoutReading()
    {
        SeedItem();

        var result = await new GetTrainingGroundsQueryHandler(_queries, DrivingInstructor(Guid.CreateVersion7()))
            .Handle(new GetTrainingGroundsQuery(Guid.CreateVersion7()), TestContext.Current.CancellationToken);

        result.Value.Should().BeEmpty();
        _queries.LastListCall.Should().BeNull();
    }

    [Fact]
    public async Task Get_GroundOfAnotherBranch_ReturnsTrainingGroundNotFound()
    {
        var item = SeedItem(Guid.CreateVersion7());

        var result = await new GetTrainingGroundQueryHandler(_queries, DrivingInstructor(Guid.CreateVersion7()))
            .Handle(new GetTrainingGroundQuery(item.Id), TestContext.Current.CancellationToken);

        result.Error.Should().Be(ApplicationErrors.NotFound<TrainingGround>());
    }

    [Fact]
    public async Task Get_SharedGround_IsVisibleToAnInstructorOfAnyBranch()
    {
        var item = SeedItem();

        var result = await new GetTrainingGroundQueryHandler(_queries, DrivingInstructor(Guid.CreateVersion7()))
            .Handle(new GetTrainingGroundQuery(item.Id), TestContext.Current.CancellationToken);

        result.Value.Should().Be(item);
    }

    [Fact]
    public void CreateValidator_EmptyCityAndStreet_ReportsBoth()
    {
        var codes = new CreateTrainingGroundCommandValidator()
            .Validate(new CreateTrainingGroundCommand("Autodrome", " ", null))
            .Errors.Select(failure => failure.ErrorCode);

        codes.Should().Equal(DomainErrors.Address.CityEmpty.Code, DomainErrors.Address.StreetEmpty.Code);
    }
}
