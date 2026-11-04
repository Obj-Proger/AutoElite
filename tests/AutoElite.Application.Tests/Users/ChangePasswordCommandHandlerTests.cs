using AutoElite.Application.Users.Authentication;
using AutoElite.Application.Users.ChangePassword;
using AutoElite.Domain.Entities;
using AutoElite.Domain.Enums;

namespace AutoElite.Application.Tests.Users;

public class ChangePasswordCommandHandlerTests
{
    private const string CurrentPassword = "old-secret-123";
    private const string NewPassword = "new-secret-456";

    private readonly FakeUserRepository _users = new();
    private readonly FakeUserRoleRepository _roles = new();
    private readonly FakeIdentityService _identity = new();
    private readonly FakeRefreshTokenStore _refreshTokens = new();

    private Task<Result<AuthTokens>> Handle(User user, string currentPassword = CurrentPassword)
    {
        var currentUser = new FakeCurrentUser { UserId = user.Id, Roles = new HashSet<UserRole> { UserRole.Student } };

        return new ChangePasswordCommandHandler(
                _users,
                _identity,
                _refreshTokens,
                new SessionIssuer(_roles, new FakeAccessTokenFactory(), _refreshTokens),
                currentUser,
                TestData.Clock())
            .Handle(new ChangePasswordCommand(currentPassword, NewPassword), TestContext.Current.CancellationToken);
    }

    private User SeedUser()
    {
        var user = TestData.NewUser();
        _users.Seed(user);
        _identity.Seed(user.Id, user.Email);
        _identity.Activate(user.Id, CurrentPassword);

        return user;
    }

    [Fact]
    public async Task Handle_CorrectCurrentPassword_ChangesItEndsOtherSessionsAndStartsANewOne()
    {
        var user = SeedUser();
        var otherSession = await _refreshTokens.IssueAsync(user.Id, TestData.Now, TestContext.Current.CancellationToken);

        var result = await Handle(user);

        result.IsSuccess.Should().BeTrue();
        _identity.Accounts[user.Id].Password.Should().Be(NewPassword);
        _refreshTokens.Tokens[otherSession.Value].IsRevoked.Should().BeTrue();
        _refreshTokens.Tokens[result.Value.RefreshToken.Value].IsRevoked.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_WrongCurrentPassword_ReturnsCurrentPasswordWrongAndKeepsSessions()
    {
        var user = SeedUser();
        var session = await _refreshTokens.IssueAsync(user.Id, TestData.Now, TestContext.Current.CancellationToken);

        var result = await Handle(user, "wrong-123");

        result.Error.Should().Be(ApplicationErrors.Account.CurrentPasswordWrong);
        _identity.Accounts[user.Id].Password.Should().Be(CurrentPassword);
        _refreshTokens.Tokens[session.Value].IsRevoked.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_DeactivatedUser_ReturnsUserDeactivated()
    {
        var user = SeedUser();
        user.Deactivate(TestData.Now);

        var result = await Handle(user);

        result.Error.Should().Be(ApplicationErrors.Account.UserDeactivated);
    }

    [Fact]
    public async Task Handle_UserMissing_ReturnsUserNotFound()
    {
        var result = await Handle(TestData.NewUser());

        result.Error.Should().Be(ApplicationErrors.NotFound<User>());
    }
}
