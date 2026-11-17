namespace AutoElite.Application.Leads.ReopenLead;

/// <summary>
/// Reopens the lead on behalf of the caller. The manager of the lead must have room under their limit of leads in
/// progress, unless the caller is an administrator.
/// </summary>
internal sealed class ReopenLeadCommandHandler(
    LeadAccess leadAccess,
    LeadCapacity leadCapacity,
    ICurrentUser currentUser,
    TimeProvider timeProvider) : ICommandHandler<ReopenLeadCommand>
{
    /// <inheritdoc />
    public async Task<Result> Handle(ReopenLeadCommand command, CancellationToken cancellationToken)
    {
        var leadResult = await leadAccess.LoadAsync(command.LeadId, cancellationToken);
        if (leadResult.IsFailure)
            return Result.Failure(leadResult.Error);

        var lead = leadResult.Value;
        if (lead is { Status: LeadStatus.Lost, ManagerId: { } managerId })
        {
            var capacityResult = await leadCapacity.CheckAsync(managerId, cancellationToken);
            if (capacityResult.IsFailure)
                return capacityResult;
        }

        return lead.Reopen(currentUser.GetRequiredUserId(), timeProvider.GetUtcNow().UtcDateTime);
    }
}
