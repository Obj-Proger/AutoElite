using AutoElite.Application.Users;
using AutoElite.Application.Users.Authentication;
using Microsoft.AspNetCore.Identity;

namespace AutoElite.Infrastructure.Identity;

/// <summary>
/// Manages the sign-in accounts with ASP.NET Core Identity. Changes are tracked and saved by the unit of work of the
/// request, except for a failed sign-in, whose count must hold although the sign-in fails.
/// </summary>
/// <remarks>
/// Tokens sent by email depend on the security stamp of the account, which changes whenever the password or the email
/// address does, so accepting an invitation, resetting the password and changing the address each invalidate every
/// token sent before. Password rules are checked by the application first (<see cref="PasswordPolicy"/>); the identity
/// system is configured with the same limits, so its rejection of a password is a defect and is thrown.
/// </remarks>
internal sealed class IdentityService(UserManager<UserAccount> userManager, IUnitOfWork unitOfWork) : IIdentityService
{
    /// <summary>The purpose an invitation token is issued for.</summary>
    internal const string InvitationPurpose = "AcceptInvitation";

    /// <inheritdoc />
    public async Task CreateAccountAsync(Guid userId, Email email, CancellationToken cancellationToken = default)
    {
        var account = new UserAccount { Id = userId, UserName = email.Value, Email = email.Value };

        EnsureSucceeded(await userManager.CreateAsync(account), "create the account");
    }

    /// <inheritdoc />
    public async Task<bool> IsActivatedAsync(Guid userId, CancellationToken cancellationToken = default) =>
        await userManager.HasPasswordAsync(await AccountAsync(userId));

    /// <inheritdoc />
    public async Task<string> GenerateInvitationTokenAsync(Guid userId, CancellationToken cancellationToken = default) =>
        await userManager.GenerateUserTokenAsync(
            await AccountAsync(userId), InvitationTokenProvider.ProviderName, InvitationPurpose);

    /// <inheritdoc />
    public async Task<bool> AcceptInvitationAsync(
        Guid userId, string token, string password, CancellationToken cancellationToken = default)
    {
        var account = await AccountAsync(userId);
        if (!await userManager.VerifyUserTokenAsync(account, InvitationTokenProvider.ProviderName, InvitationPurpose, token))
            return false;

        EnsureSucceeded(await userManager.AddPasswordAsync(account, password), "set the first password");
        account.EmailConfirmed = true;
        EnsureSucceeded(await userManager.UpdateAsync(account), "confirm the email address");

        return true;
    }

    /// <inheritdoc />
    public async Task<PasswordCheckResult> CheckPasswordAsync(
        Guid userId, string password, CancellationToken cancellationToken = default)
    {
        var account = await AccountAsync(userId);
        if (await userManager.IsLockedOutAsync(account))
            return PasswordCheckResult.LockedOut;

        if (await userManager.CheckPasswordAsync(account, password))
        {
            EnsureSucceeded(await userManager.ResetAccessFailedCountAsync(account), "reset the failed sign-ins");

            return account.EmailConfirmed ? PasswordCheckResult.Succeeded : PasswordCheckResult.EmailNotConfirmed;
        }

        // An account without a password cannot be signed in to, so wrong guesses at it are not counted.
        if (!await userManager.HasPasswordAsync(account))
            return PasswordCheckResult.WrongPassword;

        EnsureSucceeded(await userManager.AccessFailedAsync(account), "count the failed sign-in");
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return await userManager.IsLockedOutAsync(account) ? PasswordCheckResult.LockedOut : PasswordCheckResult.WrongPassword;
    }

    /// <inheritdoc />
    public async Task<string> GeneratePasswordResetTokenAsync(Guid userId, CancellationToken cancellationToken = default) =>
        await userManager.GeneratePasswordResetTokenAsync(await AccountAsync(userId));

    /// <inheritdoc />
    /// <remarks>The link proves that the user reads the mail of the account, so a lockout ends as well.</remarks>
    public async Task<bool> ResetPasswordAsync(
        Guid userId, string token, string newPassword, CancellationToken cancellationToken = default)
    {
        var account = await AccountAsync(userId);
        if (IsInvalidToken(await userManager.ResetPasswordAsync(account, token, newPassword), "reset the password"))
            return false;

        account.EmailConfirmed = true;
        account.AccessFailedCount = 0;
        account.LockoutEnd = null;
        EnsureSucceeded(await userManager.UpdateAsync(account), "confirm the email address");

        return true;
    }

    /// <inheritdoc />
    public async Task<bool> ChangePasswordAsync(
        Guid userId, string currentPassword, string newPassword, CancellationToken cancellationToken = default)
    {
        var result = await userManager.ChangePasswordAsync(await AccountAsync(userId), currentPassword, newPassword);
        if (result.Errors.Any(error => error.Code == userManager.ErrorDescriber.PasswordMismatch().Code))
            return false;

        EnsureSucceeded(result, "change the password");

        return true;
    }

    /// <inheritdoc />
    public async Task<string> GenerateEmailChangeTokenAsync(
        Guid userId, Email newEmail, CancellationToken cancellationToken = default) =>
        await userManager.GenerateChangeEmailTokenAsync(await AccountAsync(userId), newEmail.Value);

    /// <inheritdoc />
    public async Task<bool> ChangeEmailAsync(
        Guid userId, Email newEmail, string token, CancellationToken cancellationToken = default)
    {
        var account = await AccountAsync(userId);
        if (IsInvalidToken(await userManager.ChangeEmailAsync(account, newEmail.Value, token), "change the email address"))
            return false;

        EnsureSucceeded(await userManager.SetUserNameAsync(account, newEmail.Value), "change the sign-in name");

        return true;
    }

    private async Task<UserAccount> AccountAsync(Guid userId) =>
        await userManager.FindByIdAsync(userId.ToString())
        ?? throw new InvalidOperationException($"The user {userId} has no sign-in account.");

    /// <summary>Tells whether the operation failed only because the token was wrong or expired.</summary>
    private bool IsInvalidToken(IdentityResult result, string operation)
    {
        if (result.Errors.Any(error => error.Code == userManager.ErrorDescriber.InvalidToken().Code))
            return true;

        EnsureSucceeded(result, operation);

        return false;
    }

    private static void EnsureSucceeded(IdentityResult result, string operation)
    {
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(
                $"Could not {operation}: {string.Join("; ", result.Errors.Select(error => $"{error.Code}: {error.Description}"))}");
        }
    }
}
