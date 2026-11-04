namespace AutoElite.Application.Branches;

/// <summary>Reads branches for queries.</summary>
public interface IBranchQueries
{
    /// <summary>Reads all branches, sorted by name. A school has few branches, so the list is not paged.</summary>
    /// <param name="isActive">Only active or only deactivated branches; <see langword="null"/> for all.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    Task<IReadOnlyList<BranchItem>> GetListAsync(bool? isActive, CancellationToken cancellationToken = default);

    /// <summary>Reads one branch.</summary>
    /// <param name="branchId">The identifier of the branch.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    /// <returns>The branch, or <see langword="null"/> if it does not exist.</returns>
    Task<BranchItem?> GetAsync(Guid branchId, CancellationToken cancellationToken = default);
}
