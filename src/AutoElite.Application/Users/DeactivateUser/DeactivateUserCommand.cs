namespace AutoElite.Application.Users.DeactivateUser;

/// <summary>
/// Deactivates a user (FR-1.6): the data stays for history, but the user can no longer sign in, and every session
/// ends at once. Managers deactivate only students of their branch; nobody deactivates their own account, and the
/// school keeps at least one active administrator.
/// </summary>
/// <param name="UserId">The identifier of the user.</param>
[RequireRoles(UserRole.Administrator, UserRole.Manager)]
public sealed record DeactivateUserCommand(Guid UserId) : ICommand;
