namespace AutoElite.Application.Leads.StartLeadWork;

/// <summary>
/// Moves the lead to the in-progress status on behalf of the caller. A manager who takes on a lead without a manager
/// becomes its manager. The manager of the lead must have room under their limit of leads in progress, unless the caller
/// is an administrator.
/// </summary>
internal sealed class StartLeadWorkCommandHandler(
    LeadAccess leadAccess,
    LeadManagers leadManagers,
    LeadCapacity leadCapacity,
    ICurrentUser currentUser,
    TimeProvider timeProvider) : ICommandHandler<StartLeadWorkCommand>
{
    /// <inheritdoc />
    public async Task<Result> Handle(StartLeadWorkCommand command, CancellationToken cancellationToken)
    {
        var leadResult = await leadAccess.LoadAsync(command.LeadId, cancellationToken);
        if (leadResult.IsFailure)
            return Result.Failure(leadResult.Error);

        var lead = leadResult.Value;
        var callerId = currentUser.GetRequiredUserId();
        var utcNow = timeProvider.GetUtcNow().UtcDateTime;
        if (lead.Status != LeadStatus.New)
            return lead.StartWork(callerId, utcNow);

        var takesOn = lead.ManagerId is null && currentUser.IsInRole(UserRole.Manager);
        if (takesOn)
        {
            var managerResult = await leadManagers.CheckAsync(callerId, lead.BranchId, cancellationToken);
            if (managerResult.IsFailure)
                return managerResult;
        }

        if ((takesOn ? callerId : lead.ManagerId) is { } managerId)
        {
            var capacityResult = await leadCapacity.CheckAsync(managerId, cancellationToken);
            if (capacityResult.IsFailure)
                return capacityResult;
        }

        if (takesOn)
        {
            var assignResult = lead.AssignManager(callerId, callerId, utcNow);
            if (assignResult.IsFailure)
                return assignResult;
        }

        return lead.StartWork(callerId, utcNow);
    }
}
