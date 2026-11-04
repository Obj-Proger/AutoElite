using AutoElite.Application.Users.Authentication;

namespace AutoElite.Application.Users.ChangePassword;

/// <summary>
/// Changes the password of the signed-in user. All sessions end, including those on other devices, and the caller
/// gets the tokens of a new session, so it stays signed in.
/// </summary>
/// <param name="CurrentPassword">The current password.</param>
/// <param name="NewPassword">The new password; see <see cref="PasswordPolicy"/>.</param>
public sealed record ChangePasswordCommand(string? CurrentPassword, string? NewPassword) : ICommand<AuthTokens>;
