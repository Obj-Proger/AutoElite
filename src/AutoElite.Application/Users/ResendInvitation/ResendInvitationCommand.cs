namespace AutoElite.Application.Users.ResendInvitation;

/// <summary>
/// Sends a new invitation to a user who has not set a password yet, for example because the link expired.
/// The previous link stays valid until it expires.
/// </summary>
/// <param name="UserId">The identifier of the invited user.</param>
[RequireRoles(UserRole.Administrator, UserRole.Manager)]
public sealed record ResendInvitationCommand(Guid UserId) : ICommand;
