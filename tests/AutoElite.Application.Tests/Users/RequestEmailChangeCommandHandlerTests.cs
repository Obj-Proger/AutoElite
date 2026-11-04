using AutoElite.Application.Users.RequestEmailChange;
using AutoElite.Domain.Entities;

namespace AutoElite.Application.Tests.Users;

public class RequestEmailChangeCommandHandlerTests
{
    private const string Password = "secret-123";

    private readonly FakeUserRepository _users = new();
    private readonly FakeIdentityService _identity = new();
    private readonly FakeEmailQueue _emails = new();

    private Task<Result> Handle(User user, string newEmail, string password = Password) =>
        new RequestEmailChangeCommandHandler(
                _users, _identity, new FakeClientLinks(), _emails, new FakeCurrentUser { UserId = user.Id })
            .Handle(new RequestEmailChangeCommand(newEmail, password), TestContext.Current.CancellationToken);

    private User SeedUser()
    {
        var user = TestData.NewUser("ivan@example.com");
        _users.Seed(user);
        _identity.Seed(user.Id, user.Email);
        _identity.Activate(user.Id, Password);

        return user;
    }

    [Fact]
    public async Task Handle_NewFreeAddress_SendsConfirmationLinkThereAndKeepsTheCurrentAddress()
    {
        var user = SeedUser();

        var result = await Handle(user, " Ivan.New@Example.com ");

        result.IsSuccess.Should().BeTrue();
        var message = _emails.Messages.Should().ContainSingle().Which;
        message.To.Value.Should().Be("ivan.new@example.com");
        message.Subject.Should().Be("Подтвердите новый адрес электронной почты");
        message.Body.Should().Contain($"https://app.test/confirm-email?user={user.Id}&email=ivan.new@example.com&token=email-1");
        user.Email.Value.Should().Be("ivan@example.com");
    }

    [Fact]
    public async Task Handle_SameAddress_ReturnsEmailUnchanged()
    {
        var user = SeedUser();

        var result = await Handle(user, "IVAN@example.com");

        result.Error.Should().Be(DomainErrors.User.EmailUnchanged);
        _emails.Messages.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WrongPassword_ReturnsCurrentPasswordWrong()
    {
        var user = SeedUser();

        var result = await Handle(user, "ivan.new@example.com", "wrong-123");

        result.Error.Should().Be(ApplicationErrors.Account.CurrentPasswordWrong);
        _emails.Messages.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_LockedOutAccount_ReturnsLockedOut()
    {
        var user = SeedUser();
        _identity.Accounts[user.Id].IsLockedOut = true;

        var result = await Handle(user, "ivan.new@example.com");

        result.Error.Should().Be(ApplicationErrors.Account.LockedOut);
    }

    [Fact]
    public async Task Handle_AddressOfAnotherUser_ReturnsEmailTaken()
    {
        var user = SeedUser();
        _users.Seed(TestData.NewUser("petr@example.com"));

        var result = await Handle(user, "petr@example.com");

        result.Error.Should().Be(ApplicationErrors.Account.EmailTaken);
        _emails.Messages.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_DeactivatedUser_ReturnsUserDeactivated()
    {
        var user = SeedUser();
        user.Deactivate(TestData.Now);

        var result = await Handle(user, "ivan.new@example.com");

        result.Error.Should().Be(ApplicationErrors.Account.UserDeactivated);
    }
}
