namespace AutoElite.Application.Users.ResetPassword;

/// <summary>
/// Sets a new password from the emailed reset link and ends all of the user's sessions, in case the old password
/// is known to someone else.
/// </summary>
/// <param name="UserId">The identifier of the user, from the reset link.</param>
/// <param name="Token">The reset token, from the reset link.</param>
/// <param name="NewPassword">The new password; see <see cref="PasswordPolicy"/>.</param>
[AllowAnonymousRequest]
public sealed record ResetPasswordCommand(Guid UserId, string? Token, string? NewPassword) : ICommand;
