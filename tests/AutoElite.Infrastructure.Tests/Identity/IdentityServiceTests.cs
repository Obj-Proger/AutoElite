using AutoElite.Application.Users;
using AutoElite.Application.Users.Authentication;
using AutoElite.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace AutoElite.Infrastructure.Tests.Identity;

public sealed class IdentityServiceTests(DatabaseFixture database) : IClassFixture<DatabaseFixture>
{
    private const string Password = "first-password-1";
    private const string NewPassword = "second-password-2";

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    /// <summary>Runs one request: resolves the identity service in a scope of its own and saves if asked.</summary>
    private static async Task<T> RequestAsync<T>(
        ServiceProvider services, Func<IIdentityService, Task<T>> action, bool save = true)
    {
        await using var scope = services.CreateAsyncScope();
        var result = await action(scope.ServiceProvider.GetRequiredService<IIdentityService>());
        if (save)
            await scope.ServiceProvider.GetRequiredService<IUnitOfWork>().SaveChangesAsync(CancellationToken);

        return result;
    }

    /// <summary>Invites a new user: the user and the account are saved together, as the invitation does.</summary>
    private static async Task<User> InviteAsync(ServiceProvider services)
    {
        var user = User.Create(
                FullName.Create("Иванов", "Иван").Value, Email.Create($"{Guid.NewGuid():N}@example.com").Value, null, null,
                FixedClock.Default.UtcNow)
            .Value;
        await using var scope = services.CreateAsyncScope();
        scope.ServiceProvider.GetRequiredService<IUserRepository>().Add(user);
        await scope.ServiceProvider.GetRequiredService<IIdentityService>().CreateAccountAsync(user.Id, user.Email, CancellationToken);
        await scope.ServiceProvider.GetRequiredService<IUnitOfWork>().SaveChangesAsync(CancellationToken);

        return user;
    }

    private static async Task<User> ActivatedAsync(ServiceProvider services)
    {
        var user = await InviteAsync(services);
        var token = await RequestAsync(services, identity => identity.GenerateInvitationTokenAsync(user.Id, CancellationToken));
        (await RequestAsync(services, identity => identity.AcceptInvitationAsync(user.Id, token, Password, CancellationToken)))
            .Should().BeTrue();

        return user;
    }

    private static async Task<UserAccount> AccountAsync(ServiceProvider services, Guid userId)
    {
        await using var scope = services.CreateAsyncScope();

        return (await scope.ServiceProvider.GetRequiredService<UserManager<UserAccount>>().FindByIdAsync(userId.ToString()))!;
    }

    private static Task<PasswordCheckResult> CheckAsync(ServiceProvider services, Guid userId, string password) =>
        RequestAsync(services, identity => identity.CheckPasswordAsync(userId, password, CancellationToken), save: false);

    [Fact]
    public async Task InvitedUser_CannotSignInUntilTheInvitationIsAccepted()
    {
        await using var services = database.CreateServices();
        var user = await InviteAsync(services);

        (await RequestAsync(services, identity => identity.IsActivatedAsync(user.Id, CancellationToken))).Should().BeFalse();
        (await CheckAsync(services, user.Id, Password)).Should().Be(PasswordCheckResult.WrongPassword);

        var token = await RequestAsync(services, identity => identity.GenerateInvitationTokenAsync(user.Id, CancellationToken));
        (await RequestAsync(services, identity => identity.AcceptInvitationAsync(user.Id, token, Password, CancellationToken)))
            .Should().BeTrue();

        (await RequestAsync(services, identity => identity.IsActivatedAsync(user.Id, CancellationToken))).Should().BeTrue();
        (await CheckAsync(services, user.Id, Password)).Should().Be(PasswordCheckResult.Succeeded);
        var account = await AccountAsync(services, user.Id);
        account.EmailConfirmed.Should().BeTrue();
        account.UserName.Should().Be(user.Email.Value);
    }

    [Fact]
    public async Task AcceptInvitation_WrongOrUsedToken_ReturnsFalse()
    {
        await using var services = database.CreateServices();
        var user = await InviteAsync(services);
        var token = await RequestAsync(services, identity => identity.GenerateInvitationTokenAsync(user.Id, CancellationToken));

        (await RequestAsync(services, identity => identity.AcceptInvitationAsync(user.Id, "wrong", Password, CancellationToken)))
            .Should().BeFalse();
        (await RequestAsync(services, identity => identity.AcceptInvitationAsync(user.Id, token, Password, CancellationToken)))
            .Should().BeTrue();
        var reaccept = await RequestAsync(
            services, identity => identity.AcceptInvitationAsync(user.Id, token, NewPassword, CancellationToken));

        reaccept.Should().BeFalse();
    }

    [Fact]
    public async Task CheckPassword_WrongPasswordsInARow_LockTheAccount()
    {
        await using var services = database.CreateServices();
        var user = await ActivatedAsync(services);

        for (var attempt = 1; attempt < DependencyInjection.MaxFailedSignIns; attempt++)
            (await CheckAsync(services, user.Id, "wrong-password-0")).Should().Be(PasswordCheckResult.WrongPassword);

        (await CheckAsync(services, user.Id, "wrong-password-0")).Should().Be(PasswordCheckResult.LockedOut);
        (await CheckAsync(services, user.Id, Password)).Should().Be(PasswordCheckResult.LockedOut);
    }

    [Fact]
    public async Task CheckPassword_RightPasswordAfterWrongOnes_StartsTheCountAgain()
    {
        await using var services = database.CreateServices();
        var user = await ActivatedAsync(services);
        (await CheckAsync(services, user.Id, "wrong-password-0")).Should().Be(PasswordCheckResult.WrongPassword);

        (await RequestAsync(services, identity => identity.CheckPasswordAsync(user.Id, Password, CancellationToken)))
            .Should().Be(PasswordCheckResult.Succeeded);

        (await AccountAsync(services, user.Id)).AccessFailedCount.Should().Be(0);
    }

    [Fact]
    public async Task CheckPassword_UnconfirmedEmail_ReturnsEmailNotConfirmed()
    {
        await using var services = database.CreateServices();
        var user = await ActivatedAsync(services);
        await using (var scope = services.CreateAsyncScope())
        {
            var accounts = scope.ServiceProvider.GetRequiredService<UserManager<UserAccount>>();
            var account = (await accounts.FindByIdAsync(user.Id.ToString()))!;
            account.EmailConfirmed = false;
            await accounts.UpdateAsync(account);
            await scope.ServiceProvider.GetRequiredService<IUnitOfWork>().SaveChangesAsync(CancellationToken);
        }

        (await CheckAsync(services, user.Id, Password)).Should().Be(PasswordCheckResult.EmailNotConfirmed);
    }

    [Fact]
    public async Task ResetPassword_ReplacesThePasswordOnce()
    {
        await using var services = database.CreateServices();
        var user = await ActivatedAsync(services);
        var token = await RequestAsync(services, identity => identity.GeneratePasswordResetTokenAsync(user.Id, CancellationToken));

        (await RequestAsync(services, identity => identity.ResetPasswordAsync(user.Id, "wrong", NewPassword, CancellationToken)))
            .Should().BeFalse();
        (await RequestAsync(services, identity => identity.ResetPasswordAsync(user.Id, token, NewPassword, CancellationToken)))
            .Should().BeTrue();

        (await CheckAsync(services, user.Id, NewPassword)).Should().Be(PasswordCheckResult.Succeeded);
        (await CheckAsync(services, user.Id, Password)).Should().Be(PasswordCheckResult.WrongPassword);
        (await RequestAsync(services, identity => identity.ResetPasswordAsync(user.Id, token, Password, CancellationToken)))
            .Should().BeFalse();
    }

    [Fact]
    public async Task ChangePassword_NeedsTheCurrentPassword()
    {
        await using var services = database.CreateServices();
        var user = await ActivatedAsync(services);

        var withWrongPassword = await RequestAsync(
            services, identity => identity.ChangePasswordAsync(user.Id, "wrong-password-0", NewPassword, CancellationToken));

        withWrongPassword.Should().BeFalse();
        (await RequestAsync(services, identity => identity.ChangePasswordAsync(user.Id, Password, NewPassword, CancellationToken)))
            .Should().BeTrue();

        (await CheckAsync(services, user.Id, NewPassword)).Should().Be(PasswordCheckResult.Succeeded);
    }

    [Fact]
    public async Task ChangeEmail_TokenIsBoundToTheNewAddress()
    {
        await using var services = database.CreateServices();
        var user = await ActivatedAsync(services);
        var newEmail = Email.Create($"{Guid.NewGuid():N}@example.com").Value;
        var otherEmail = Email.Create($"{Guid.NewGuid():N}@example.com").Value;
        var token = await RequestAsync(
            services, identity => identity.GenerateEmailChangeTokenAsync(user.Id, newEmail, CancellationToken));

        (await RequestAsync(services, identity => identity.ChangeEmailAsync(user.Id, otherEmail, token, CancellationToken)))
            .Should().BeFalse();
        (await RequestAsync(services, identity => identity.ChangeEmailAsync(user.Id, newEmail, token, CancellationToken)))
            .Should().BeTrue();

        var account = await AccountAsync(services, user.Id);
        account.Email.Should().Be(newEmail.Value);
        account.UserName.Should().Be(newEmail.Value);
        account.EmailConfirmed.Should().BeTrue();
    }
}
