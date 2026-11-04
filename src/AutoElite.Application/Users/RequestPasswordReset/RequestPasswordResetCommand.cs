namespace AutoElite.Application.Users.RequestPasswordReset;

/// <summary>
/// Emails a link to set a new password (FR-1.4). It always succeeds, so it cannot be used to find out which email
/// addresses have accounts. A user who has not accepted the invitation yet gets the invitation again instead.
/// </summary>
/// <param name="Email">The email address of the account.</param>
[AllowAnonymousRequest]
public sealed record RequestPasswordResetCommand(string? Email) : ICommand;
