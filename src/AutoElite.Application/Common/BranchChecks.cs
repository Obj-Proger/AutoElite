namespace AutoElite.Application.Common;

/// <summary>Checks the branch that a user, a lead or another record is tied to.</summary>
internal static class BranchChecks
{
    /// <summary>
    /// Checks that the branch, if any, exists inside the caller's scope and is active. A branch outside the scope is
    /// reported as not found.
    /// </summary>
    /// <param name="branchRepository">The branch repository.</param>
    /// <param name="branchId">The branch, or <see langword="null"/> for none.</param>
    /// <param name="scope">The caller's data scope.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    public static async Task<Result> CheckAsync(
        IRepository<Branch> branchRepository, Guid? branchId, DataScope scope, CancellationToken cancellationToken)
    {
        if (branchId is not { } id)
            return Result.Success();

        var branch = scope.Includes(id) ? await branchRepository.GetByIdAsync(id, cancellationToken) : null;
        if (branch is null)
            return Result.Failure(ApplicationErrors.NotFound<Branch>());

        return branch.IsActive ? Result.Success() : Result.Failure(ApplicationErrors.Branch.Inactive);
    }
}
