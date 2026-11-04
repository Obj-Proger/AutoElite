namespace AutoElite.Application.Leads.ChangeLeadBranch;

/// <summary>Checks the branch in the caller's scope and moves the lead.</summary>
internal sealed class ChangeLeadBranchCommandHandler(
    LeadAccess leadAccess,
    IRepository<Branch> branchRepository,
    ICurrentUser currentUser) : ICommandHandler<ChangeLeadBranchCommand>
{
    /// <inheritdoc />
    public async Task<Result> Handle(ChangeLeadBranchCommand command, CancellationToken cancellationToken)
    {
        var leadResult = await leadAccess.LoadAsync(command.LeadId, cancellationToken);
        if (leadResult.IsFailure)
            return Result.Failure(leadResult.Error);

        var branchResult = await BranchChecks.CheckAsync(
            branchRepository, command.BranchId, DataScope.For(currentUser), cancellationToken);
        if (branchResult.IsFailure)
            return branchResult;

        return leadResult.Value.AssignBranch(command.BranchId);
    }
}
