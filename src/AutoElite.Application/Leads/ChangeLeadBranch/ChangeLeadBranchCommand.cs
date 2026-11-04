namespace AutoElite.Application.Leads.ChangeLeadBranch;

/// <summary>
/// Hands an open lead to another branch, or returns it to the common pool when no branch is given. Managers can move
/// leads only within their scope: to their own branch or to the pool.
/// </summary>
/// <param name="LeadId">The identifier of the lead.</param>
/// <param name="BranchId">The new branch, or <see langword="null"/> for none.</param>
[RequireRoles(UserRole.Administrator, UserRole.Manager)]
public sealed record ChangeLeadBranchCommand(Guid LeadId, Guid? BranchId) : ICommand;
