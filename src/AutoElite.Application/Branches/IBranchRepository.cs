namespace AutoElite.Application.Branches;

/// <summary>Loads and adds branches for commands.</summary>
public interface IBranchRepository : IRepository<Branch>
{
    /// <summary>Determines whether another branch, active or not, already has the name, ignoring case.</summary>
    /// <param name="name">The trimmed name.</param>
    /// <param name="exceptBranchId">A branch to leave out, when the name of that branch is being changed.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    Task<bool> NameExistsAsync(string name, Guid? exceptBranchId = null, CancellationToken cancellationToken = default);
}
