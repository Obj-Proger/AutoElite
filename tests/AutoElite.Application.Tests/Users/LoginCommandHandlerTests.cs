using AutoElite.Application.Users.Authentication;
using AutoElite.Application.Users.Login;
using AutoElite.Domain.Entities;
using AutoElite.Domain.Enums;

namespace AutoElite.Application.Tests.Users;

public class LoginCommandHandlerTests
{
    private const string Password = "secret-123";

    private readonly FakeUserRepository _users = new();
    private readonly FakeUserRoleRepository _roles = new();
    private readonly FakeIdentityService _identity = new();
    private readonly FakeRefreshTokenStore _refreshTokens = new();

    private Task<Result<AuthTokens>> Handle(string? email, string password = Password) =>
        new LoginCommandHandler(
                _users,
                _identity,
                new SessionIssuer(_roles, new FakeAccessTokenFactory(), _refreshTokens),
                TestData.Clock())
            .Handle(new LoginCommand(email, password), TestContext.Current.CancellationToken);

    private User SeedActivatedUser(params UserRole[] roles)
    {
        var user = TestData.NewUser("ivan@example.com");
        _users.Seed(user);
        _identity.Seed(user.Id, user.Email);
        _identity.Activate(user.Id, Password);
        foreach (var role in roles)
            _roles.Seed(user.Id, role);

        return user;
    }

    [Fact]
    public async Task Handle_CorrectPassword_IssuesAccessTokenWithRolesAndRefreshToken()
    {
        var user = SeedActivatedUser(UserRole.Student, UserRole.Manager);

        var result = await Handle(" Ivan@Example.com ");

        result.IsSuccess.Should().BeTrue();
        result.Value.AccessToken.Should().Be(
            new IssuedToken($"access:{user.Id}:Student,Manager", TestData.Now + FakeAccessTokenFactory.Lifetime));
        result.Value.RefreshToken.ExpiresAtUtc.Should().Be(TestData.Now + FakeRefreshTokenStore.Lifetime);
        _refreshTokens.Tokens[result.Value.RefreshToken.Value].UserId.Should().Be(user.Id);
    }

    [Fact]
    public async Task Handle_WrongPassword_ReturnsInvalidCredentials()
    {
        SeedActivatedUser();

        var result = await Handle("ivan@example.com", "wrong-123");

        result.Error.Should().Be(ApplicationErrors.Account.InvalidCredentials);
        _refreshTokens.Tokens.Should().BeEmpty();
    }

    [Theory]
    [InlineData("nobody@example.com")]
    [InlineData("not-an-email")]
    public async Task Handle_UnknownOrMalformedEmail_ReturnsInvalidCredentials(string email)
    {
        SeedActivatedUser();

        var result = await Handle(email);

        result.Error.Should().Be(ApplicationErrors.Account.InvalidCredentials);
    }

    [Fact]
    public async Task Handle_InvitationNotAccepted_ReturnsInvalidCredentials()
    {
        var user = TestData.NewUser("ivan@example.com");
        _users.Seed(user);
        _identity.Seed(user.Id, user.Email);

        var result = await Handle("ivan@example.com");

        result.Error.Should().Be(ApplicationErrors.Account.InvalidCredentials);
    }

    [Fact]
    public async Task Handle_LockedOutAccount_ReturnsLockedOut()
    {
        var user = SeedActivatedUser();
        _identity.Accounts[user.Id].IsLockedOut = true;

        var result = await Handle("ivan@example.com");

        result.Error.Should().Be(ApplicationErrors.Account.LockedOut);
    }

    [Fact]
    public async Task Handle_EmailNotConfirmed_ReturnsEmailNotConfirmed()
    {
        var user = SeedActivatedUser();
        _identity.Accounts[user.Id].EmailConfirmed = false;

        var result = await Handle("ivan@example.com");

        result.Error.Should().Be(ApplicationErrors.Account.EmailNotConfirmed);
    }

    [Fact]
    public async Task Handle_DeactivatedUserWithCorrectPassword_ReturnsUserDeactivated()
    {
        var user = SeedActivatedUser();
        user.Deactivate(TestData.Now);

        var result = await Handle("ivan@example.com");

        result.Error.Should().Be(ApplicationErrors.Account.UserDeactivated);
        _refreshTokens.Tokens.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_DeactivatedUserWithWrongPassword_ReturnsInvalidCredentials()
    {
        var user = SeedActivatedUser();
        user.Deactivate(TestData.Now);

        var result = await Handle("ivan@example.com", "wrong-123");

        result.Error.Should().Be(ApplicationErrors.Account.InvalidCredentials);
    }
}
