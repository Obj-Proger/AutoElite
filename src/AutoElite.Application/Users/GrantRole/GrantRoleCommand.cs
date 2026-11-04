namespace AutoElite.Application.Users.GrantRole;

/// <summary>
/// Grants a role to a user (FR-1.5). The user gets it in the access token at the next renewal of the session.
/// </summary>
/// <param name="UserId">The identifier of the user.</param>
/// <param name="Role">The role to grant.</param>
[RequireRoles(UserRole.Administrator)]
public sealed record GrantRoleCommand(Guid UserId, UserRole Role) : ICommand;
