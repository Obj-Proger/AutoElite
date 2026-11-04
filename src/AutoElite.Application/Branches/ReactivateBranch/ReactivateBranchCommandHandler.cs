namespace AutoElite.Application.Branches.ReactivateBranch;

/// <summary>Reactivates the branch.</summary>
internal sealed class ReactivateBranchCommandHandler(IBranchRepository branchRepository) : ICommandHandler<ReactivateBranchCommand>
{
    /// <inheritdoc />
    public async Task<Result> Handle(ReactivateBranchCommand command, CancellationToken cancellationToken)
    {
        var branch = await branchRepository.GetByIdAsync(command.BranchId, cancellationToken);

        return branch is null ? Result.Failure(ApplicationErrors.NotFound<Branch>()) : branch.Reactivate();
    }
}
