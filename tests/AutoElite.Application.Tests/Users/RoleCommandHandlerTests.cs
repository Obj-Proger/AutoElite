using AutoElite.Application.Users.GrantRole;
using AutoElite.Application.Users.RevokeRole;
using AutoElite.Domain.Entities;
using AutoElite.Domain.Enums;

namespace AutoElite.Application.Tests.Users;

public class RoleCommandHandlerTests
{
    private readonly UserManagementFixture _fixture = new();

    private Task<Result> Grant(Guid userId, UserRole role, FakeCurrentUser caller) =>
        new GrantRoleCommandHandler(_fixture.CreateManagedUsers(caller), _fixture.Roles)
            .Handle(new GrantRoleCommand(userId, role), TestContext.Current.CancellationToken);

    private Task<Result> Revoke(Guid userId, UserRole role, FakeCurrentUser caller) =>
        new RevokeRoleCommandHandler(_fixture.CreateManagedUsers(caller), _fixture.Roles)
            .Handle(new RevokeRoleCommand(userId, role), TestContext.Current.CancellationToken);

    private async Task<IReadOnlyList<UserRole>> RolesOf(User user) =>
        await _fixture.Roles.GetRolesAsync(user.Id, TestContext.Current.CancellationToken);

    [Fact]
    public async Task Grant_CompatibleRole_AddsIt()
    {
        var caller = _fixture.SeedAdministrator();
        var user = _fixture.SeedUser("ivan@example.com", null, UserRole.DrivingInstructor);

        var result = await Grant(user.Id, UserRole.TheoryInstructor, caller);

        result.IsSuccess.Should().BeTrue();
        (await RolesOf(user)).Should().Equal(UserRole.DrivingInstructor, UserRole.TheoryInstructor);
    }

    [Fact]
    public async Task Grant_InstructorRoleToStudent_ReturnsIncompatibleRoles()
    {
        var caller = _fixture.SeedAdministrator();
        var user = _fixture.SeedUser("ivan@example.com", null, UserRole.Student);

        var result = await Grant(user.Id, UserRole.DrivingInstructor, caller);

        result.Error.Should().Be(DomainErrors.UserRoleAssignment.IncompatibleRoles);
        (await RolesOf(user)).Should().Equal(UserRole.Student);
    }

    [Fact]
    public async Task Grant_RoleAlreadyHeld_ReturnsAlreadyAssigned()
    {
        var caller = _fixture.SeedAdministrator();
        var user = _fixture.SeedUser("ivan@example.com", null, UserRole.Manager);

        var result = await Grant(user.Id, UserRole.Manager, caller);

        result.Error.Should().Be(DomainErrors.UserRoleAssignment.AlreadyAssigned);
    }

    [Fact]
    public async Task Grant_UnknownUser_ReturnsUserNotFound()
    {
        var result = await Grant(Guid.CreateVersion7(), UserRole.Manager, _fixture.SeedAdministrator());

        result.Error.Should().Be(ApplicationErrors.NotFound<User>());
    }

    [Fact]
    public async Task Revoke_HeldRole_RemovesIt()
    {
        var caller = _fixture.SeedAdministrator();
        var user = _fixture.SeedUser("ivan@example.com", null, UserRole.Manager, UserRole.Student);

        var result = await Revoke(user.Id, UserRole.Manager, caller);

        result.IsSuccess.Should().BeTrue();
        (await RolesOf(user)).Should().Equal(UserRole.Student);
    }

    [Fact]
    public async Task Revoke_RoleNotHeld_ReturnsNotAssigned()
    {
        var caller = _fixture.SeedAdministrator();
        var user = _fixture.SeedUser("ivan@example.com", null, UserRole.Student);

        var result = await Revoke(user.Id, UserRole.Manager, caller);

        result.Error.Should().Be(DomainErrors.UserRoleAssignment.NotAssigned);
    }

    [Fact]
    public async Task Revoke_LastActiveAdministrator_ReturnsLastAdministrator()
    {
        var caller = _fixture.SeedAdministrator();

        var result = await Revoke(caller.UserId!.Value, UserRole.Administrator, caller);

        result.Error.Should().Be(DomainErrors.UserRoleAssignment.LastAdministrator);
        _fixture.Roles.Items.Should().ContainSingle();
    }

    [Fact]
    public async Task Revoke_AdministratorWhenAnotherIsActive_RemovesIt()
    {
        var caller = _fixture.SeedAdministrator();
        var other = _fixture.SeedUser("second-admin@example.com", null, UserRole.Administrator);

        var result = await Revoke(other.Id, UserRole.Administrator, caller);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Revoke_AdministratorRoleOfDeactivatedUser_IgnoresTheActiveCount()
    {
        var caller = _fixture.SeedAdministrator();
        var other = _fixture.SeedUser("old-admin@example.com", null, UserRole.Administrator);
        other.Deactivate(TestData.Now);

        var result = await Revoke(other.Id, UserRole.Administrator, caller);

        result.IsSuccess.Should().BeTrue();
    }
}
