namespace AutoElite.Application.Branches.GetBranch;

/// <summary>Reads one branch.</summary>
/// <param name="BranchId">The identifier of the branch.</param>
public sealed record GetBranchQuery(Guid BranchId) : IQuery<BranchItem>;
