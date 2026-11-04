namespace AutoElite.Application.Users.AcceptInvitation;

/// <summary>
/// Sets the first password of an invited user. Following the emailed link proves that the address is the user's,
/// so the email is confirmed at the same time and the user can sign in.
/// </summary>
/// <param name="UserId">The identifier of the user, from the invitation link.</param>
/// <param name="Token">The invitation token, from the invitation link.</param>
/// <param name="Password">The new password; see <see cref="PasswordPolicy"/>.</param>
[AllowAnonymousRequest]
public sealed record AcceptInvitationCommand(Guid UserId, string? Token, string? Password) : ICommand;
