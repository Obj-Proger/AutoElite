namespace AutoElite.Application.Users.ReactivateUser;

/// <summary>
/// Reactivates a deactivated user, who can sign in again with the old password. Managers reactivate only students
/// of their branch.
/// </summary>
/// <param name="UserId">The identifier of the user.</param>
[RequireRoles(UserRole.Administrator, UserRole.Manager)]
public sealed record ReactivateUserCommand(Guid UserId) : ICommand;
