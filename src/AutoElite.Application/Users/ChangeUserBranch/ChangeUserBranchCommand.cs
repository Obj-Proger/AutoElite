namespace AutoElite.Application.Users.ChangeUserBranch;

/// <summary>Moves a user to another branch, or unties the user from any branch (FR-1.9).</summary>
/// <param name="UserId">The identifier of the user.</param>
/// <param name="BranchId">The new branch, or <see langword="null"/> for none.</param>
[RequireRoles(UserRole.Administrator)]
public sealed record ChangeUserBranchCommand(Guid UserId, Guid? BranchId) : ICommand;
