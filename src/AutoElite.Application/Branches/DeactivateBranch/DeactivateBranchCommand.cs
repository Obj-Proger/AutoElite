namespace AutoElite.Application.Branches.DeactivateBranch;

/// <summary>
/// Closes a branch. Its data stays for history; users, groups and vehicles tied to it keep the link, but nothing new
/// can be tied to it.
/// </summary>
/// <param name="BranchId">The identifier of the branch.</param>
[RequireRoles(UserRole.Administrator)]
public sealed record DeactivateBranchCommand(Guid BranchId) : ICommand;
