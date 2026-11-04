namespace AutoElite.Application.Branches.DeactivateBranch;

/// <summary>Deactivates the branch.</summary>
internal sealed class DeactivateBranchCommandHandler(IBranchRepository branchRepository) : ICommandHandler<DeactivateBranchCommand>
{
    /// <inheritdoc />
    public async Task<Result> Handle(DeactivateBranchCommand command, CancellationToken cancellationToken)
    {
        var branch = await branchRepository.GetByIdAsync(command.BranchId, cancellationToken);

        return branch is null ? Result.Failure(ApplicationErrors.NotFound<Branch>()) : branch.Deactivate();
    }
}
