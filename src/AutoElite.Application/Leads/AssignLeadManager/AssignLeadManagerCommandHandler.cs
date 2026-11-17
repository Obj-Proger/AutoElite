namespace AutoElite.Application.Leads.AssignLeadManager;

/// <summary>
/// Checks the manager and assigns the lead. A lead in progress goes only to a manager with room under their limit of leads
/// in progress, unless the caller is an administrator.
/// </summary>
internal sealed class AssignLeadManagerCommandHandler(
    LeadAccess leadAccess,
    LeadManagers leadManagers,
    LeadCapacity leadCapacity,
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

        if (lead.Status == LeadStatus.InProgress && lead.ManagerId != command.ManagerId)
        {
            var capacityResult = await leadCapacity.CheckAsync(command.ManagerId, cancellationToken);
            if (capacityResult.IsFailure)
                return capacityResult;
        }

        return lead.AssignManager(command.ManagerId, currentUser.GetRequiredUserId(), timeProvider.GetUtcNow().UtcDateTime);
    }
}
