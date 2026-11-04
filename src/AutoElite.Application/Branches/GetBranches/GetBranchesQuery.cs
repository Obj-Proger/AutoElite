namespace AutoElite.Application.Branches.GetBranches;

/// <summary>
/// Reads the branches of the school, for example to choose one in a form. Any signed-in user may read them,
/// because names and addresses of branches are public.
/// </summary>
/// <param name="IsActive">Only active or only closed branches; <see langword="null"/> for all.</param>
public sealed record GetBranchesQuery(bool? IsActive = null) : IQuery<IReadOnlyList<BranchItem>>;
