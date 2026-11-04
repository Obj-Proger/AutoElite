using AutoElite.Application.Users.ConfirmEmailChange;
using AutoElite.Domain.Entities;
using AutoElite.Domain.Events;

namespace AutoElite.Application.Tests.Users;

public class ConfirmEmailChangeCommandHandlerTests
{
    private const string NewEmail = "ivan.new@example.com";

    private readonly FakeUserRepository _users = new();
    private readonly FakeIdentityService _identity = new();

    private Task<Result> Handle(Guid userId, string token, string newEmail = NewEmail) =>
        new ConfirmEmailChangeCommandHandler(_users, _identity, TestData.Clock())
            .Handle(new ConfirmEmailChangeCommand(userId, newEmail, token), TestContext.Current.CancellationToken);

    private async Task<(User User, string Token)> SeedChangeRequestAsync()
    {
        var user = TestData.NewUser("ivan@example.com");
        _users.Seed(user);
        user.ClearDomainEvents();
        _identity.Seed(user.Id, user.Email);
        _identity.Activate(user.Id, "secret-123");
        var token = await _identity.GenerateEmailChangeTokenAsync(
            user.Id, Email.Create(NewEmail).Value, TestContext.Current.CancellationToken);

        return (user, token);
    }

    [Fact]
    public async Task Handle_ValidLink_ChangesTheUserAndAccountAddress()
    {
        var (user, token) = await SeedChangeRequestAsync();

        var result = await Handle(user.Id, token);

        result.IsSuccess.Should().BeTrue();
        user.Email.Value.Should().Be(NewEmail);
        _identity.Accounts[user.Id].Email.Value.Should().Be(NewEmail);
        user.DomainEvents.Should().ContainSingle().Which.Should().Be(
            new UserEmailChangedDomainEvent(user.Id, "ivan@example.com", NewEmail, TestData.Now));
    }

    [Fact]
    public async Task Handle_WrongToken_ReturnsEmailChangeInvalid()
    {
        var (user, _) = await SeedChangeRequestAsync();

        var result = await Handle(user.Id, "forged");

        result.Error.Should().Be(ApplicationErrors.Account.EmailChangeInvalid);
        _identity.Accounts[user.Id].Email.Value.Should().Be("ivan@example.com");
    }

    [Fact]
    public async Task Handle_TokenForAnotherAddress_ReturnsEmailChangeInvalid()
    {
        var (user, token) = await SeedChangeRequestAsync();

        var result = await Handle(user.Id, token, "someone.else@example.com");

        result.Error.Should().Be(ApplicationErrors.Account.EmailChangeInvalid);
    }

    [Fact]
    public async Task Handle_AddressTakenSinceTheRequest_ReturnsEmailTaken()
    {
        var (user, token) = await SeedChangeRequestAsync();
        _users.Seed(TestData.NewUser(NewEmail));

        var result = await Handle(user.Id, token);

        result.Error.Should().Be(ApplicationErrors.Account.EmailTaken);
    }

    [Fact]
    public async Task Handle_LinkUsedAgain_ReturnsEmailChangeInvalid()
    {
        var (user, token) = await SeedChangeRequestAsync();
        await Handle(user.Id, token);

        var result = await Handle(user.Id, token);

        result.Error.Should().Be(ApplicationErrors.Account.EmailChangeInvalid);
    }

    [Theory]
    [InlineData(true, NewEmail)]
    [InlineData(false, "not-an-email")]
    public async Task Handle_DeactivatedUserOrMalformedAddress_ReturnsEmailChangeInvalid(bool deactivate, string newEmail)
    {
        var (user, token) = await SeedChangeRequestAsync();
        if (deactivate)
            user.Deactivate(TestData.Now);

        var result = await Handle(user.Id, token, newEmail);

        result.Error.Should().Be(ApplicationErrors.Account.EmailChangeInvalid);
    }
}
