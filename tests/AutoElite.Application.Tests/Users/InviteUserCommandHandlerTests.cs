using AutoElite.Application.Users.InviteUser;
using AutoElite.Domain.Entities;
using AutoElite.Domain.Enums;
using AutoElite.Domain.Events;

namespace AutoElite.Application.Tests.Users;

public class InviteUserCommandHandlerTests
{
    private readonly FakeUserRepository _users = new();
    private readonly InMemoryRepository<UserRoleAssignment> _roles = new();
    private readonly InMemoryRepository<Branch> _branches = new();
    private readonly FakeIdentityService _identity = new();

    private Task<Result<Guid>> Handle(InviteUserCommand command, FakeCurrentUser currentUser) =>
        new InviteUserCommandHandler(_users, _roles, _branches, _identity, currentUser, TestData.Clock())
            .Handle(command, TestContext.Current.CancellationToken);

    private static InviteUserCommand Command(Guid? branchId = null, params UserRole[] roles)
    {
        IReadOnlyList<UserRole> grantedRoles = roles.Length == 0 ? [UserRole.Student] : roles;

        return new InviteUserCommand(
            "Petrov", "Petr", "Petrovich", "  Petr@Example.com ", "+79991234567", branchId, grantedRoles, Language.English);
    }

    [Fact]
    public async Task Handle_AdministratorInvitesInstructor_CreatesUserRoleAndAccount()
    {
        var result = await Handle(Command(null, UserRole.DrivingInstructor), TestData.Administrator());

        result.IsSuccess.Should().BeTrue();
        var user = _users.Items.Should().ContainSingle().Which;
        user.Id.Should().Be(result.Value);
        user.FullName.Should().Be(FullName.Create("Petrov", "Petr", "Petrovich").Value);
        user.Email.Value.Should().Be("petr@example.com");
        user.Phone!.Value.Should().Be("+79991234567");
        user.BranchId.Should().BeNull();
        user.PreferredLanguage.Should().Be(Language.English);
        user.DomainEvents.Should().ContainSingle().Which.Should().BeOfType<UserRegisteredDomainEvent>()
            .Which.OccurredOnUtc.Should().Be(TestData.Now);

        var assignment = _roles.Items.Should().ContainSingle().Which;
        assignment.UserId.Should().Be(user.Id);
        assignment.Role.Should().Be(UserRole.DrivingInstructor);

        _identity.Accounts[user.Id].Email.Should().Be(user.Email);
    }

    [Fact]
    public async Task Handle_AdministratorGrantsSeveralCompatibleRoles_CreatesAnAssignmentForEach()
    {
        var result = await Handle(Command(null, UserRole.Student, UserRole.Manager), TestData.Administrator());

        result.IsSuccess.Should().BeTrue();
        _roles.Items.Select(assignment => assignment.Role).Should().Equal(UserRole.Student, UserRole.Manager);
    }

    [Fact]
    public async Task Handle_IncompatibleRoles_ReturnsPolicyErrorAndCreatesNothing()
    {
        var result = await Handle(Command(null, UserRole.Student, UserRole.DrivingInstructor), TestData.Administrator());

        result.Error.Should().Be(DomainErrors.UserRoleAssignment.IncompatibleRoles);
        _users.Items.Should().BeEmpty();
        _roles.Items.Should().BeEmpty();
        _identity.Accounts.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_ManagerInvitesStudentWithoutBranch_AssignsTheManagersBranch()
    {
        var branch = TestData.NewBranch();
        _branches.Seed(branch);

        var result = await Handle(Command(), TestData.Manager(branch.Id));

        result.IsSuccess.Should().BeTrue();
        _users.Items.Single().BranchId.Should().Be(branch.Id);
    }

    [Fact]
    public async Task Handle_ManagerInvitesInstructor_ReturnsForbiddenError()
    {
        var result = await Handle(Command(null, UserRole.TheoryInstructor), TestData.Manager(null));

        result.Error.Should().Be(ApplicationErrors.Account.ManagerInvitesOnlyStudents);
        _users.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_ManagerInvitesIntoAnotherBranch_ReturnsBranchNotFound()
    {
        var ownBranch = TestData.NewBranch("Central");
        var otherBranch = TestData.NewBranch("North");
        _branches.Seed(ownBranch);
        _branches.Seed(otherBranch);

        var result = await Handle(Command(otherBranch.Id), TestData.Manager(ownBranch.Id));

        result.Error.Should().Be(ApplicationErrors.NotFound<Branch>());
        _users.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_UnknownBranch_ReturnsBranchNotFound()
    {
        var result = await Handle(Command(Guid.CreateVersion7()), TestData.Administrator());

        result.Error.Should().Be(ApplicationErrors.NotFound<Branch>());
    }

    [Fact]
    public async Task Handle_InactiveBranch_ReturnsBranchInactiveError()
    {
        var branch = TestData.NewBranch();
        branch.Deactivate();
        _branches.Seed(branch);

        var result = await Handle(Command(branch.Id), TestData.Administrator());

        result.Error.Should().Be(ApplicationErrors.Account.BranchInactive);
    }

    [Fact]
    public async Task Handle_EmailAlreadyUsed_ReturnsEmailTakenError()
    {
        _users.Seed(TestData.NewUser("petr@example.com"));

        var result = await Handle(Command(), TestData.Administrator());

        result.Error.Should().Be(ApplicationErrors.Account.EmailTaken);
        _users.Items.Should().HaveCount(1);
        _identity.Accounts.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_EmptyPhone_CreatesUserWithoutPhone()
    {
        var command = Command() with { Phone = "  " };

        await Handle(command, TestData.Administrator());

        _users.Items.Single().Phone.Should().BeNull();
    }
}
