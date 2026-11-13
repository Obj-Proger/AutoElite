using AutoElite.Application.Common.Authorization;
using AutoElite.Application.Users;
using AutoElite.Application.Users.InviteFirstAdministrator;
using AutoElite.Domain.Enums;
using AutoElite.Domain.Events;

namespace AutoElite.Application.Tests.Users;

public class InviteFirstAdministratorCommandHandlerTests
{
    private static readonly InviteFirstAdministratorCommand _command =
        new("Petrova", "Anna", "  Anna@Example.com ", Language.Russian);

    private readonly FakeUserRepository _users = new();
    private readonly FakeUserRoleRepository _roles;
    private readonly FakeIdentityService _identity = new();

    public InviteFirstAdministratorCommandHandlerTests() => _roles = new FakeUserRoleRepository(_users);

    private Task<Result<Guid?>> Handle(InviteFirstAdministratorCommand command) =>
        new InviteFirstAdministratorCommandHandler(new UserRegistration(_users, _roles, _identity, TestData.Clock()), _roles)
            .Handle(command, TestContext.Current.CancellationToken);

    [Fact]
    public async Task Handle_SchoolWithoutAdministrators_RegistersTheAdministratorWithAnInvitation()
    {
        var result = await Handle(_command);

        result.IsSuccess.Should().BeTrue();
        var user = _users.Items.Should().ContainSingle().Which;
        result.Value.Should().Be(user.Id);
        user.Email.Value.Should().Be("anna@example.com");
        user.PreferredLanguage.Should().Be(Language.Russian);
        user.DomainEvents.Should().ContainSingle().Which.Should().BeOfType<UserRegisteredDomainEvent>();
        _roles.Items.Should().ContainSingle().Which.Role.Should().Be(UserRole.Administrator);
        _identity.Accounts.Should().ContainKey(user.Id);
    }

    [Fact]
    public async Task Handle_SchoolWithAnActiveAdministrator_ChangesNothing()
    {
        var administrator = TestData.NewUser("admin@example.com");
        _users.Seed(administrator);
        _roles.Seed(administrator.Id, UserRole.Administrator);

        var result = await Handle(_command);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeNull();
        _users.Items.Should().ContainSingle().Which.Should().BeSameAs(administrator);
        _identity.Accounts.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_OnlyADeactivatedAdministrator_InvitesTheAdministratorOfTheSettings()
    {
        var former = TestData.NewUser("former@example.com");
        former.Deactivate(TestData.Now).IsSuccess.Should().BeTrue();
        _users.Seed(former);
        _roles.Seed(former.Id, UserRole.Administrator);

        var result = await Handle(_command);

        result.Value.Should().NotBeNull();
        _users.Items.Should().HaveCount(2);
    }

    [Fact]
    public async Task Handle_AddressOfAnotherUser_ReturnsTheErrorAndCreatesNothing()
    {
        _users.Seed(TestData.NewUser("anna@example.com"));

        var result = await Handle(_command);

        result.Error.Should().Be(ApplicationErrors.Account.EmailTaken);
        _roles.Items.Should().BeEmpty();
        _identity.Accounts.Should().BeEmpty();
    }

    [Fact]
    public void Command_IsForTheSystemOnly()
    {
        typeof(InviteFirstAdministratorCommand).IsDefined(typeof(SystemRequestAttribute), inherit: false).Should().BeTrue();
    }

    [Fact]
    public void Validator_WithoutTheDetails_ReportsEachField()
    {
        var command = new InviteFirstAdministratorCommand(null, " ", "not an address");

        var failures = new InviteFirstAdministratorCommandValidator().Validate(command);

        failures.Errors.Select(failure => failure.PropertyName).Distinct().Should().BeEquivalentTo(
            new[]
            {
                nameof(InviteFirstAdministratorCommand.LastName),
                nameof(InviteFirstAdministratorCommand.FirstName),
                nameof(InviteFirstAdministratorCommand.Email)
            });
    }
}
