using AutoElite.Application.Users.DeactivateUser;
using AutoElite.Application.Users.ReactivateUser;
using AutoElite.Domain.Entities;
using AutoElite.Domain.Enums;

namespace AutoElite.Application.Tests.Users;

public class UserActivationCommandHandlerTests
{
    private readonly UserManagementFixture _fixture = new();

    private Task<Result> Deactivate(Guid userId, FakeCurrentUser caller) =>
        new DeactivateUserCommandHandler(
                _fixture.CreateManagedUsers(caller), _fixture.Roles, _fixture.RefreshTokens, caller, TestData.Clock())
            .Handle(new DeactivateUserCommand(userId), TestContext.Current.CancellationToken);

    private Task<Result> Reactivate(Guid userId, FakeCurrentUser caller) =>
        new ReactivateUserCommandHandler(_fixture.CreateManagedUsers(caller))
            .Handle(new ReactivateUserCommand(userId), TestContext.Current.CancellationToken);

    [Fact]
    public async Task Deactivate_ActiveUser_DeactivatesAndEndsAllSessions()
    {
        var caller = _fixture.SeedAdministrator();
        var user = _fixture.SeedUser("ivan@example.com", null, UserRole.DrivingInstructor);
        var session = await _fixture.RefreshTokens.IssueAsync(user.Id, TestData.Now, TestContext.Current.CancellationToken);

        var result = await Deactivate(user.Id, caller);

        result.IsSuccess.Should().BeTrue();
        user.IsActive.Should().BeFalse();
        _fixture.RefreshTokens.Tokens[session.Value].IsRevoked.Should().BeTrue();
    }

    [Fact]
    public async Task Deactivate_OwnAccount_ReturnsCannotDeactivateSelf()
    {
        var caller = _fixture.SeedAdministrator();
        _fixture.SeedUser("second-admin@example.com", null, UserRole.Administrator);

        var result = await Deactivate(caller.UserId!.Value, caller);

        result.Error.Should().Be(ApplicationErrors.Account.CannotDeactivateSelf);
    }

    [Fact]
    public async Task Deactivate_LastActiveAdministrator_ReturnsLastAdministrator()
    {
        var caller = new FakeCurrentUser
        {
            UserId = Guid.CreateVersion7(),
            Roles = new HashSet<UserRole> { UserRole.Administrator }
        };
        var onlyAdministrator = _fixture.SeedUser("admin@example.com", null, UserRole.Administrator);

        var result = await Deactivate(onlyAdministrator.Id, caller);

        result.Error.Should().Be(DomainErrors.UserRoleAssignment.LastAdministrator);
        onlyAdministrator.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task Deactivate_ManagerDeactivatesStudentOfOwnBranch_Deactivates()
    {
        var branchId = Guid.CreateVersion7();
        var student = _fixture.SeedUser("ivan@example.com", branchId, UserRole.Student);

        var result = await Deactivate(student.Id, TestData.Manager(branchId));

        result.IsSuccess.Should().BeTrue();
        student.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task Deactivate_ManagerDeactivatesInstructor_ReturnsManagerManagesOnlyStudents()
    {
        var instructor = _fixture.SeedUser("ivan@example.com", null, UserRole.DrivingInstructor);

        var result = await Deactivate(instructor.Id, TestData.Manager(null));

        result.Error.Should().Be(ApplicationErrors.Account.ManagerManagesOnlyStudents);
        instructor.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task Deactivate_ManagerDeactivatesStudentOfAnotherBranch_ReturnsUserNotFound()
    {
        var student = _fixture.SeedUser("ivan@example.com", Guid.CreateVersion7(), UserRole.Student);

        var result = await Deactivate(student.Id, TestData.Manager(Guid.CreateVersion7()));

        result.Error.Should().Be(ApplicationErrors.NotFound<User>());
    }

    [Fact]
    public async Task Deactivate_AlreadyInactiveUser_ReturnsAlreadyInactive()
    {
        var caller = _fixture.SeedAdministrator();
        var user = _fixture.SeedUser("ivan@example.com", null, UserRole.Student);
        user.Deactivate(TestData.Now);

        var result = await Deactivate(user.Id, caller);

        result.Error.Should().Be(DomainErrors.User.AlreadyInactive);
    }

    [Fact]
    public async Task Reactivate_DeactivatedUser_ReactivatesIt()
    {
        var caller = _fixture.SeedAdministrator();
        var user = _fixture.SeedUser("ivan@example.com", null, UserRole.Student);
        user.Deactivate(TestData.Now);

        var result = await Reactivate(user.Id, caller);

        result.IsSuccess.Should().BeTrue();
        user.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task Reactivate_ActiveUser_ReturnsAlreadyActive()
    {
        var caller = _fixture.SeedAdministrator();
        var user = _fixture.SeedUser("ivan@example.com", null, UserRole.Student);

        var result = await Reactivate(user.Id, caller);

        result.Error.Should().Be(DomainErrors.User.AlreadyActive);
    }
}
