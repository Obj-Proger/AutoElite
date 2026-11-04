using AutoElite.Application.Users.Invitations;
using AutoElite.Application.Users.ResendInvitation;
using AutoElite.Domain.Entities;

namespace AutoElite.Application.Tests.Users;

public class ResendInvitationCommandHandlerTests
{
    private readonly FakeUserRepository _users = new();
    private readonly FakeIdentityService _identity = new();
    private readonly FakeEmailQueue _emails = new();

    private Task<Result> Handle(Guid userId, FakeCurrentUser currentUser) =>
        new ResendInvitationCommandHandler(
                _users, _identity, new InvitationSender(_identity, new FakeClientLinks(), _emails), currentUser)
            .Handle(new ResendInvitationCommand(userId), TestContext.Current.CancellationToken);

    private User SeedInvitedUser(Guid? branchId = null)
    {
        var user = TestData.NewUser(branchId: branchId);
        _users.Seed(user);
        _identity.Seed(user.Id, user.Email);

        return user;
    }

    [Fact]
    public async Task Handle_UserWithoutPassword_QueuesANewInvitation()
    {
        var user = SeedInvitedUser();

        var result = await Handle(user.Id, TestData.Administrator());

        result.IsSuccess.Should().BeTrue();
        _emails.Messages.Should().ContainSingle().Which.To.Should().Be(user.Email);
        _identity.Accounts[user.Id].InvitationTokens.Should().ContainSingle();
    }

    [Fact]
    public async Task Handle_UnknownUser_ReturnsUserNotFound()
    {
        var result = await Handle(Guid.CreateVersion7(), TestData.Administrator());

        result.Error.Should().Be(ApplicationErrors.NotFound<User>());
    }

    [Fact]
    public async Task Handle_UserOfAnotherBranch_ReturnsUserNotFoundToTheManager()
    {
        var user = SeedInvitedUser(Guid.CreateVersion7());

        var result = await Handle(user.Id, TestData.Manager(Guid.CreateVersion7()));

        result.Error.Should().Be(ApplicationErrors.NotFound<User>());
        _emails.Messages.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_DeactivatedUser_ReturnsCannotModifyInactive()
    {
        var user = SeedInvitedUser();
        user.Deactivate(TestData.Now);

        var result = await Handle(user.Id, TestData.Administrator());

        result.Error.Should().Be(DomainErrors.User.CannotModifyInactive);
    }

    [Fact]
    public async Task Handle_ActivatedUser_ReturnsAlreadyActivated()
    {
        var user = SeedInvitedUser();
        _identity.Activate(user.Id, "secret-123");

        var result = await Handle(user.Id, TestData.Administrator());

        result.Error.Should().Be(ApplicationErrors.Account.AlreadyActivated);
        _emails.Messages.Should().BeEmpty();
    }
}
