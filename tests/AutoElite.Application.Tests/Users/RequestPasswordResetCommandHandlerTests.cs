using AutoElite.Application.Users.Invitations;
using AutoElite.Application.Users.RequestPasswordReset;
using AutoElite.Domain.Entities;
using AutoElite.Domain.Enums;

namespace AutoElite.Application.Tests.Users;

public class RequestPasswordResetCommandHandlerTests
{
    private readonly FakeUserRepository _users = new();
    private readonly FakeIdentityService _identity = new();
    private readonly FakeEmailQueue _emails = new();

    private Task<Result> Handle(string email)
    {
        var links = new FakeClientLinks();

        return new RequestPasswordResetCommandHandler(
                _users, _identity, new InvitationSender(_identity, links, _emails), links, _emails)
            .Handle(new RequestPasswordResetCommand(email), TestContext.Current.CancellationToken);
    }

    private User SeedUser(bool activated, Language language = Language.Russian)
    {
        var user = TestData.NewUser("ivan@example.com", language: language);
        _users.Seed(user);
        _identity.Seed(user.Id, user.Email);
        if (activated)
            _identity.Activate(user.Id, "secret-123");

        return user;
    }

    [Fact]
    public async Task Handle_ActivatedUser_QueuesResetLinkInTheUsersLanguage()
    {
        var user = SeedUser(activated: true);

        var result = await Handle(" Ivan@Example.com ");

        result.IsSuccess.Should().BeTrue();
        var message = _emails.Messages.Should().ContainSingle().Which;
        message.To.Should().Be(user.Email);
        message.Subject.Should().Be("Сброс пароля в AutoElite");
        message.Body.Should().Contain($"https://app.test/reset-password?user={user.Id}&token=reset-1");
    }

    [Fact]
    public async Task Handle_EnglishSpeakingUser_QueuesEnglishResetEmail()
    {
        SeedUser(activated: true, Language.English);

        await Handle("ivan@example.com");

        _emails.Messages.Should().ContainSingle().Which.Subject.Should().Be("Resetting your AutoElite password");
    }

    [Fact]
    public async Task Handle_InvitationNotAccepted_SendsTheInvitationAgain()
    {
        var user = SeedUser(activated: false);

        await Handle("ivan@example.com");

        _emails.Messages.Should().ContainSingle().Which.Body.Should().Contain("https://app.test/invitation?");
        _identity.Accounts[user.Id].ResetTokens.Should().BeEmpty();
    }

    [Theory]
    [InlineData("nobody@example.com")]
    [InlineData("not-an-email")]
    public async Task Handle_UnknownOrMalformedEmail_SucceedsWithoutSending(string email)
    {
        SeedUser(activated: true);

        var result = await Handle(email);

        result.IsSuccess.Should().BeTrue();
        _emails.Messages.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_DeactivatedUser_SucceedsWithoutSending()
    {
        var user = SeedUser(activated: true);
        user.Deactivate(TestData.Now);

        var result = await Handle("ivan@example.com");

        result.IsSuccess.Should().BeTrue();
        _emails.Messages.Should().BeEmpty();
    }
}
