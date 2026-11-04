using AutoElite.Application.Users.Authentication;

namespace AutoElite.Application.Users;

/// <summary>
/// The sign-in account of a user: password, email confirmation and the tokens sent by email. The infrastructure
/// implements it with ASP.NET Core Identity; the account shares its identifier with the <see cref="User"/>.
/// </summary>
/// <remarks>Changes to accounts are tracked like aggregate changes and saved by <see cref="IUnitOfWork"/>.</remarks>
public interface IIdentityService
{
    /// <summary>
    /// Creates the account of a new user, without a password and with an unconfirmed email address, so that the user
    /// cannot sign in until the invitation is accepted.
    /// </summary>
    /// <param name="userId">The identifier of the new user.</param>
    /// <param name="email">The user's email address, which is also the sign-in name.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    Task CreateAccountAsync(Guid userId, Email email, CancellationToken cancellationToken = default);

    /// <summary>Determines whether the user has accepted the invitation and set a password.</summary>
    /// <param name="userId">The identifier of the user.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    Task<bool> IsActivatedAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>Generates a single-purpose, time-limited token that lets the user set the first password.</summary>
    /// <param name="userId">The identifier of a user who has not accepted the invitation yet.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    Task<string> GenerateInvitationTokenAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks the invitation token and, if it is valid, sets the first password and confirms the email address.
    /// Accepting invalidates every invitation token of the user.
    /// </summary>
    /// <param name="userId">The identifier of a user who has not accepted the invitation yet.</param>
    /// <param name="token">The token from the invitation link.</param>
    /// <param name="password">A password that follows <see cref="PasswordPolicy"/>.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    /// <returns><see langword="false"/> if the token is wrong or expired; nothing is changed then.</returns>
    Task<bool> AcceptInvitationAsync(Guid userId, string token, string password, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks the password at sign-in. Wrong passwords are counted, and too many in a row lock the account for a while;
    /// the implementation saves the count at once, so it holds even though the sign-in fails.
    /// </summary>
    /// <param name="userId">The identifier of the user.</param>
    /// <param name="password">The password entered by the user.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    Task<PasswordCheckResult> CheckPasswordAsync(Guid userId, string password, CancellationToken cancellationToken = default);
}
