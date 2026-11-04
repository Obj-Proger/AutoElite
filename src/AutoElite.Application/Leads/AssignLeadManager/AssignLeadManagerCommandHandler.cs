namespace AutoElite.Application.Leads.AssignLeadManager;

/// <summary>Checks the manager and assigns the lead.</summary>
internal sealed class AssignLeadManagerCommandHandler(
    LeadAccess leadAccess,
    LeadManagers leadManagers,
    ICurrentUser currentUser,
    TimeProvider timeProvider) : ICommandHandler<AssignLeadManagerCommand>
{
    /// <inheritdoc />
    public async Task<Result> Handle(AssignLeadManagerCommand command, CancellationToken cancellationToken)
    {
        var leadResult = await leadAccess.LoadAsync(command.LeadId, cancellationToken);
        if (leadResult.IsFailure)
            return Result.Failure(leadResult.Error);

        var lead = leadResult.Value;
        var managerResult = await leadManagers.CheckAsync(command.ManagerId, lead.BranchId, cancellationToken);
        if (managerResult.IsFailure)
            return managerResult;

        return lead.AssignManager(command.ManagerId, currentUser.GetRequiredUserId(), timeProvider.GetUtcNow().UtcDateTime);
    }
}
