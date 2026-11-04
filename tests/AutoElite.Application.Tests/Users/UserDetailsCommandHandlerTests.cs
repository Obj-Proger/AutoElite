using AutoElite.Application.Users.ChangeUserBranch;
using AutoElite.Application.Users.UpdateUserDetails;
using AutoElite.Domain.Entities;
using AutoElite.Domain.Enums;

namespace AutoElite.Application.Tests.Users;

public class UserDetailsCommandHandlerTests
{
    private readonly UserManagementFixture _fixture = new();

    private Task<Result> ChangeBranch(Guid userId, Guid? branchId, FakeCurrentUser caller) =>
        new ChangeUserBranchCommandHandler(_fixture.CreateManagedUsers(caller), _fixture.Branches, caller)
            .Handle(new ChangeUserBranchCommand(userId, branchId), TestContext.Current.CancellationToken);

    private Task<Result> UpdateDetails(UpdateUserDetailsCommand command, FakeCurrentUser caller) =>
        new UpdateUserDetailsCommandHandler(_fixture.CreateManagedUsers(caller))
            .Handle(command, TestContext.Current.CancellationToken);

    [Fact]
    public async Task ChangeBranch_ActiveBranch_MovesTheUser()
    {
        var caller = _fixture.SeedAdministrator();
        var branch = TestData.NewBranch();
        _fixture.Branches.Seed(branch);
        var user = _fixture.SeedUser("ivan@example.com", null, UserRole.Manager);

        var result = await ChangeBranch(user.Id, branch.Id, caller);

        result.IsSuccess.Should().BeTrue();
        user.BranchId.Should().Be(branch.Id);
    }

    [Fact]
    public async Task ChangeBranch_NoBranch_UntiesTheUser()
    {
        var caller = _fixture.SeedAdministrator();
        var user = _fixture.SeedUser("ivan@example.com", Guid.CreateVersion7(), UserRole.Manager);

        var result = await ChangeBranch(user.Id, null, caller);

        result.IsSuccess.Should().BeTrue();
        user.BranchId.Should().BeNull();
    }

    [Fact]
    public async Task ChangeBranch_InactiveBranch_ReturnsBranchInactive()
    {
        var caller = _fixture.SeedAdministrator();
        var branch = TestData.NewBranch();
        branch.Deactivate();
        _fixture.Branches.Seed(branch);
        var user = _fixture.SeedUser("ivan@example.com", null, UserRole.Manager);

        var result = await ChangeBranch(user.Id, branch.Id, caller);

        result.Error.Should().Be(ApplicationErrors.Branch.Inactive);
        user.BranchId.Should().BeNull();
    }

    [Fact]
    public async Task ChangeBranch_UnknownBranch_ReturnsBranchNotFound()
    {
        var caller = _fixture.SeedAdministrator();
        var user = _fixture.SeedUser("ivan@example.com", null, UserRole.Manager);

        var result = await ChangeBranch(user.Id, Guid.CreateVersion7(), caller);

        result.Error.Should().Be(ApplicationErrors.NotFound<Branch>());
    }

    [Fact]
    public async Task UpdateDetails_ManagerCorrectsStudentOfOwnBranch_UpdatesNameAndPhone()
    {
        var branchId = Guid.CreateVersion7();
        var student = _fixture.SeedUser("ivan@example.com", branchId, UserRole.Student);

        var result = await UpdateDetails(
            new UpdateUserDetailsCommand(student.Id, "Ivanova", "Irina", null, "+79991234567"), TestData.Manager(branchId));

        result.IsSuccess.Should().BeTrue();
        student.FullName.Should().Be(FullName.Create("Ivanova", "Irina").Value);
        student.Phone!.Value.Should().Be("+79991234567");
    }

    [Fact]
    public async Task UpdateDetails_ManagerCorrectsAnotherManager_ReturnsManagerManagesOnlyStudents()
    {
        var other = _fixture.SeedUser("ivan@example.com", null, UserRole.Manager);

        var result = await UpdateDetails(
            new UpdateUserDetailsCommand(other.Id, "Ivanova", "Irina", null, null), TestData.Manager(null));

        result.Error.Should().Be(ApplicationErrors.Account.ManagerManagesOnlyStudents);
    }
}
