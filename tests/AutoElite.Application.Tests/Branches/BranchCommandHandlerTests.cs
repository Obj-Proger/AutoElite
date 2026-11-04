using AutoElite.Application.Branches.CreateBranch;
using AutoElite.Application.Branches.DeactivateBranch;
using AutoElite.Application.Branches.ReactivateBranch;
using AutoElite.Application.Branches.UpdateBranch;
using AutoElite.Domain.Entities;

namespace AutoElite.Application.Tests.Branches;

public class BranchCommandHandlerTests
{
    private const string TimeZoneId = "Asia/Yekaterinburg";

    private readonly FakeBranchRepository _branches = new();

    private Task<Result<Guid>> Create(string name, string? phone = null) =>
        new CreateBranchCommandHandler(_branches).Handle(
            new CreateBranchCommand(name, "Yekaterinburg", "Lenina 1", phone, TimeZoneId), TestContext.Current.CancellationToken);

    private Task<Result> Update(Guid branchId, string name, string? phone = null) =>
        new UpdateBranchCommandHandler(_branches).Handle(
            new UpdateBranchCommand(branchId, name, "Moscow", "Tverskaya 10", phone, "Europe/Moscow"),
            TestContext.Current.CancellationToken);

    private Branch Seed(string name)
    {
        var branch = TestData.NewBranch(name);
        _branches.Seed(branch);

        return branch;
    }

    [Fact]
    public async Task Create_FreeName_AddsTheBranch()
    {
        var result = await Create("  Central  ", "+73431234567");

        var branch = _branches.Items.Should().ContainSingle().Which;
        branch.Id.Should().Be(result.Value);
        branch.Name.Should().Be("Central");
        branch.Address.Should().Be(Address.Create("Yekaterinburg", "Lenina 1").Value);
        branch.Phone!.Value.Should().Be("+73431234567");
        branch.TimeZoneId.Should().Be(TimeZoneId);
    }

    [Fact]
    public async Task Create_NameOfAnotherBranchInOtherCase_ReturnsNameTaken()
    {
        Seed("Central");

        var result = await Create("CENTRAL");

        result.Error.Should().Be(ApplicationErrors.Branch.NameTaken);
        _branches.Items.Should().HaveCount(1);
    }

    [Fact]
    public async Task Update_NewDetails_AppliesThem()
    {
        var branch = Seed("Central");

        var result = await Update(branch.Id, "Moscow office");

        result.IsSuccess.Should().BeTrue();
        branch.Name.Should().Be("Moscow office");
        branch.Address.City.Should().Be("Moscow");
        branch.TimeZoneId.Should().Be("Europe/Moscow");
        branch.Phone.Should().BeNull();
    }

    [Fact]
    public async Task Update_KeepingItsOwnName_Succeeds()
    {
        var branch = Seed("Central");

        var result = await Update(branch.Id, "Central");

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Update_NameOfAnotherBranch_ReturnsNameTaken()
    {
        var branch = Seed("Central");
        Seed("North");

        var result = await Update(branch.Id, "north");

        result.Error.Should().Be(ApplicationErrors.Branch.NameTaken);
    }

    [Fact]
    public async Task Update_InactiveBranch_ReturnsCannotModifyInactive()
    {
        var branch = Seed("Central");
        branch.Deactivate();

        var result = await Update(branch.Id, "Central");

        result.Error.Should().Be(DomainErrors.Branch.CannotModifyInactive);
    }

    [Fact]
    public async Task Update_UnknownBranch_ReturnsBranchNotFound()
    {
        var result = await Update(Guid.CreateVersion7(), "Central");

        result.Error.Should().Be(ApplicationErrors.NotFound<Branch>());
    }

    [Fact]
    public async Task DeactivateAndReactivate_ToggleTheBranch()
    {
        var branch = Seed("Central");

        var deactivation = await new DeactivateBranchCommandHandler(_branches)
            .Handle(new DeactivateBranchCommand(branch.Id), TestContext.Current.CancellationToken);
        var inactiveAfterDeactivation = !branch.IsActive;
        var reactivation = await new ReactivateBranchCommandHandler(_branches)
            .Handle(new ReactivateBranchCommand(branch.Id), TestContext.Current.CancellationToken);

        deactivation.IsSuccess.Should().BeTrue();
        inactiveAfterDeactivation.Should().BeTrue();
        reactivation.IsSuccess.Should().BeTrue();
        branch.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task Deactivate_UnknownBranch_ReturnsBranchNotFound()
    {
        var result = await new DeactivateBranchCommandHandler(_branches)
            .Handle(new DeactivateBranchCommand(Guid.CreateVersion7()), TestContext.Current.CancellationToken);

        result.Error.Should().Be(ApplicationErrors.NotFound<Branch>());
    }
}
