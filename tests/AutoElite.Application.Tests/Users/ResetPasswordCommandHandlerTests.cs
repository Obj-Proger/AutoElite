using AutoElite.Application.Users.ResetPassword;
using AutoElite.Domain.Entities;

namespace AutoElite.Application.Tests.Users;

public class ResetPasswordCommandHandlerTests
{
    private const string NewPassword = "new-secret-456";

    private readonly FakeUserRepository _users = new();
    private readonly FakeIdentityService _identity = new();
    private readonly FakeRefreshTokenStore _refreshTokens = new();

    private Task<Result> Handle(Guid userId, string token) =>
        new ResetPasswordCommandHandler(_users, _identity, _refreshTokens, TestData.Clock())
            .Handle(new ResetPasswordCommand(userId, token, NewPassword), TestContext.Current.CancellationToken);

    private async Task<(User User, string Token)> SeedResetRequestAsync()
    {
        var user = TestData.NewUser();
        _users.Seed(user);
        _identity.Seed(user.Id, user.Email);
        _identity.Activate(user.Id, "old-secret-123");
        var token = await _identity.GeneratePasswordResetTokenAsync(user.Id, TestContext.Current.CancellationToken);

        return (user, token);
    }

    [Fact]
    public async Task Handle_ValidToken_SetsPasswordAndEndsAllSessions()
    {
        var (user, token) = await SeedResetRequestAsync();
        var session = await _refreshTokens.IssueAsync(user.Id, TestData.Now, TestContext.Current.CancellationToken);

        var result = await Handle(user.Id, token);

        result.IsSuccess.Should().BeTrue();
        _identity.Accounts[user.Id].Password.Should().Be(NewPassword);
        _refreshTokens.Tokens[session.Value].IsRevoked.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WrongToken_ReturnsPasswordResetInvalidAndKeepsSessions()
    {
        var (user, _) = await SeedResetRequestAsync();
        var session = await _refreshTokens.IssueAsync(user.Id, TestData.Now, TestContext.Current.CancellationToken);

        var result = await Handle(user.Id, "forged");

        result.Error.Should().Be(ApplicationErrors.Account.PasswordResetInvalid);
        _identity.Accounts[user.Id].Password.Should().Be("old-secret-123");
        _refreshTokens.Tokens[session.Value].IsRevoked.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_UnknownUser_ReturnsPasswordResetInvalid()
    {
        var result = await Handle(Guid.CreateVersion7(), "reset-1");

        result.Error.Should().Be(ApplicationErrors.Account.PasswordResetInvalid);
    }

    [Fact]
    public async Task Handle_DeactivatedUser_ReturnsPasswordResetInvalid()
    {
        var (user, token) = await SeedResetRequestAsync();
        user.Deactivate(TestData.Now);

        var result = await Handle(user.Id, token);

        result.Error.Should().Be(ApplicationErrors.Account.PasswordResetInvalid);
        _identity.Accounts[user.Id].Password.Should().Be("old-secret-123");
    }
}
