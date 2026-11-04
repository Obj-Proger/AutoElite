namespace AutoElite.Application.Branches.ReactivateBranch;

/// <summary>Reopens a closed branch.</summary>
/// <param name="BranchId">The identifier of the branch.</param>
[RequireRoles(UserRole.Administrator)]
public sealed record ReactivateBranchCommand(Guid BranchId) : ICommand;
