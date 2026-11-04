using AutoElite.Application.Users.Invitations;
using AutoElite.Domain.Entities;
using AutoElite.Domain.Enums;
using AutoElite.Domain.Events;

namespace AutoElite.Application.Tests.Users;

public class SendInvitationWhenUserRegisteredHandlerTests
{
    private readonly FakeUserRepository _users = new();
    private readonly FakeIdentityService _identity = new();
    private readonly FakeEmailQueue _emails = new();

    private Task Handle(User user) =>
        new SendInvitationWhenUserRegisteredHandler(_users, _identity, new InvitationSender(_identity, new FakeClientLinks(), _emails))
            .Handle(new UserRegisteredDomainEvent(user.Id, user.Email.Value, TestData.Now), TestContext.Current.CancellationToken);

    private User SeedInvitedUser(Language language = Language.Russian)
    {
        var user = TestData.NewUser(language: language, firstName: "Ivan");
        _users.Seed(user);
        _identity.Seed(user.Id, user.Email);

        return user;
    }

    [Fact]
    public async Task Handle_NewUser_QueuesRussianInvitationWithTheLink()
    {
        var user = SeedInvitedUser();

        await Handle(user);

        var message = _emails.Messages.Should().ContainSingle().Which;
        message.To.Should().Be(user.Email);
        message.Subject.Should().Be("Ваша учётная запись в AutoElite");
        message.Body.Should().StartWith("Здравствуйте, Ivan!")
            .And.Contain($"https://app.test/invitation?user={user.Id}&token=invitation-1");
    }

    [Fact]
    public async Task Handle_EnglishSpeakingUser_QueuesEnglishInvitation()
    {
        var user = SeedInvitedUser(Language.English);

        await Handle(user);

        var message = _emails.Messages.Should().ContainSingle().Which;
        message.Subject.Should().Be("Your AutoElite account");
        message.Body.Should().StartWith("Hello, Ivan!");
    }

    [Fact]
    public async Task Handle_UserAlreadyActivated_SendsNothing()
    {
        var user = SeedInvitedUser();
        _identity.Activate(user.Id, "secret-123");

        await Handle(user);

        _emails.Messages.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_UserDeactivated_SendsNothing()
    {
        var user = SeedInvitedUser();
        user.Deactivate(TestData.Now);

        await Handle(user);

        _emails.Messages.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_UserMissing_SendsNothing()
    {
        await Handle(TestData.NewUser());

        _emails.Messages.Should().BeEmpty();
    }
}
