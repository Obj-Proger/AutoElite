namespace AutoElite.Application.Users.RequestEmailChange;

/// <summary>
/// Starts changing the email address of the signed-in user: a confirmation link is sent to the new address.
/// The user keeps signing in with the current address until the link is followed, so a mistyped address
/// cannot lock the user out.
/// </summary>
/// <param name="NewEmail">The address the user wants to use.</param>
/// <param name="CurrentPassword">The current password, required so that an unattended session cannot take the account.</param>
public sealed record RequestEmailChangeCommand(string? NewEmail, string? CurrentPassword) : ICommand;
