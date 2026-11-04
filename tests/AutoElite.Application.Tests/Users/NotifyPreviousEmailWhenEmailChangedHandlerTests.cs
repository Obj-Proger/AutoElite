using AutoElite.Application.Users.EmailChangeNotices;
using AutoElite.Domain.Enums;
using AutoElite.Domain.Events;

namespace AutoElite.Application.Tests.Users;

public class NotifyPreviousEmailWhenEmailChangedHandlerTests
{
    private readonly FakeUserRepository _users = new();
    private readonly FakeEmailQueue _emails = new();

    private Task Handle(Guid userId) =>
        new NotifyPreviousEmailWhenEmailChangedHandler(_users, _emails).Handle(
            new UserEmailChangedDomainEvent(userId, "ivan@example.com", "ivan.new@example.com", TestData.Now),
            TestContext.Current.CancellationToken);

    [Fact]
    public async Task Handle_ChangedAddress_NotifiesThePreviousAddressInTheUsersLanguage()
    {
        var user = TestData.NewUser("ivan.new@example.com", language: Language.English);
        _users.Seed(user);

        await Handle(user.Id);

        var message = _emails.Messages.Should().ContainSingle().Which;
        message.To.Value.Should().Be("ivan@example.com");
        message.Subject.Should().Be("Your AutoElite email address was changed");
        message.Body.Should().Contain("ivan.new@example.com");
    }

    [Fact]
    public async Task Handle_UserMissing_SendsNothing()
    {
        await Handle(Guid.CreateVersion7());

        _emails.Messages.Should().BeEmpty();
    }
}
