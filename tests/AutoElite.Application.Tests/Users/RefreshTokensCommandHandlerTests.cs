using AutoElite.Application.Users.Authentication;
using AutoElite.Application.Users.RefreshTokens;
using AutoElite.Domain.Entities;
using AutoElite.Domain.Enums;

namespace AutoElite.Application.Tests.Users;

public class RefreshTokensCommandHandlerTests
{
    private readonly FakeUserRepository _users = new();
    private readonly FakeUserRoleRepository _roles = new();
    private readonly FakeRefreshTokenStore _refreshTokens = new();
    private readonly FixedTimeProvider _clock = TestData.Clock();

    private Task<Result<AuthTokens>> Handle(string token) =>
        new RefreshTokensCommandHandler(
                _users, _refreshTokens, new SessionIssuer(_roles, new FakeAccessTokenFactory(), _refreshTokens), _clock)
            .Handle(new RefreshTokensCommand(token), TestContext.Current.CancellationToken);

    private async Task<(User User, string Token)> SeedSessionAsync()
    {
        var user = TestData.NewUser();
        _users.Seed(user);
        _roles.Seed(user.Id, UserRole.Student);
        var token = await _refreshTokens.IssueAsync(user.Id, TestData.Now, TestContext.Current.CancellationToken);

        return (user, token.Value);
    }

    [Fact]
    public async Task Handle_ValidToken_UsesItUpAndIssuesNewTokensWithCurrentRoles()
    {
        var (user, token) = await SeedSessionAsync();
        _roles.Seed(user.Id, UserRole.Manager);

        var result = await Handle(token);

        result.IsSuccess.Should().BeTrue();
        result.Value.AccessToken.Value.Should().Be($"access:{user.Id}:Student,Manager");
        result.Value.RefreshToken.Value.Should().NotBe(token);
        _refreshTokens.Tokens[token].IsUsed.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_UnknownToken_ReturnsRefreshTokenInvalid()
    {
        var result = await Handle("forged");

        result.Error.Should().Be(ApplicationErrors.Account.RefreshTokenInvalid);
    }

    [Fact]
    public async Task Handle_TokenUsedTwice_RejectsItAndRevokesTheNewSession()
    {
        var (_, token) = await SeedSessionAsync();
        var renewed = await Handle(token);

        var result = await Handle(token);

        result.Error.Should().Be(ApplicationErrors.Account.RefreshTokenInvalid);
        _refreshTokens.Tokens[renewed.Value.RefreshToken.Value].IsRevoked.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_DeactivatedUser_ReturnsRefreshTokenInvalid()
    {
        var (user, token) = await SeedSessionAsync();
        user.Deactivate(TestData.Now);

        var result = await Handle(token);

        result.Error.Should().Be(ApplicationErrors.Account.RefreshTokenInvalid);
    }

    [Fact]
    public async Task Handle_ExpiredToken_ReturnsRefreshTokenInvalid()
    {
        var user = TestData.NewUser();
        _users.Seed(user);
        var token = await _refreshTokens.IssueAsync(
            user.Id, TestData.Now - FakeRefreshTokenStore.Lifetime, TestContext.Current.CancellationToken);

        var result = await Handle(token.Value);

        result.Error.Should().Be(ApplicationErrors.Account.RefreshTokenInvalid);
    }
}
