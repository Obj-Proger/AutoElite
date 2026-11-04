using AutoElite.Application.Users.AcceptInvitation;
using AutoElite.Domain.Entities;

namespace AutoElite.Application.Tests.Users;

public class AcceptInvitationCommandHandlerTests
{
    private const string Password = "secret-123";

    private readonly FakeUserRepository _users = new();
    private readonly FakeIdentityService _identity = new();

    private Task<Result> Handle(Guid userId, string token) =>
        new AcceptInvitationCommandHandler(_users, _identity)
            .Handle(new AcceptInvitationCommand(userId, token, Password), TestContext.Current.CancellationToken);

    private async Task<(User User, string Token)> SeedInvitedUserAsync()
    {
        var user = TestData.NewUser();
        _users.Seed(user);
        _identity.Seed(user.Id, user.Email);
        var token = await _identity.GenerateInvitationTokenAsync(user.Id, TestContext.Current.CancellationToken);

        return (user, token);
    }

    [Fact]
    public async Task Handle_ValidToken_SetsPasswordAndConfirmsEmail()
    {
        var (user, token) = await SeedInvitedUserAsync();

        var result = await Handle(user.Id, token);

        result.IsSuccess.Should().BeTrue();
        _identity.Accounts[user.Id].Password.Should().Be(Password);
        _identity.Accounts[user.Id].EmailConfirmed.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WrongToken_ReturnsInvitationInvalidAndSetsNoPassword()
    {
        var (user, _) = await SeedInvitedUserAsync();

        var result = await Handle(user.Id, "forged");

        result.Error.Should().Be(ApplicationErrors.Account.InvitationInvalid);
        _identity.Accounts[user.Id].Password.Should().BeNull();
    }

    [Fact]
    public async Task Handle_UnknownUser_ReturnsInvitationInvalid()
    {
        var result = await Handle(Guid.CreateVersion7(), "invitation-1");

        result.Error.Should().Be(ApplicationErrors.Account.InvitationInvalid);
    }

    [Fact]
    public async Task Handle_DeactivatedUser_ReturnsInvitationInvalid()
    {
        var (user, token) = await SeedInvitedUserAsync();
        user.Deactivate(TestData.Now);

        var result = await Handle(user.Id, token);

        result.Error.Should().Be(ApplicationErrors.Account.InvitationInvalid);
        _identity.Accounts[user.Id].Password.Should().BeNull();
    }

    [Fact]
    public async Task Handle_InvitationAlreadyAccepted_ReturnsAlreadyActivated()
    {
        var (user, token) = await SeedInvitedUserAsync();
        await Handle(user.Id, token);

        var result = await Handle(user.Id, token);

        result.Error.Should().Be(ApplicationErrors.Account.AlreadyActivated);
    }
}
