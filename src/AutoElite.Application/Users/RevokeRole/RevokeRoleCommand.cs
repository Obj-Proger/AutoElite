namespace AutoElite.Application.Users.RevokeRole;

/// <summary>
/// Takes a role away from a user (FR-1.5). The school always keeps at least one active administrator.
/// The role disappears from the access token at the next renewal of the session.
/// </summary>
/// <param name="UserId">The identifier of the user.</param>
/// <param name="Role">The role to take away.</param>
[RequireRoles(UserRole.Administrator)]
public sealed record RevokeRoleCommand(Guid UserId, UserRole Role) : ICommand;
