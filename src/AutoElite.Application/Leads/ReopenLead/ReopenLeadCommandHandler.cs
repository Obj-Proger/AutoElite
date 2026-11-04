namespace AutoElite.Application.Leads.ReopenLead;

/// <summary>Reopens the lead on behalf of the caller.</summary>
internal sealed class ReopenLeadCommandHandler(LeadAccess leadAccess, ICurrentUser currentUser, TimeProvider timeProvider)
    : ICommandHandler<ReopenLeadCommand>
{
    /// <inheritdoc />
    public async Task<Result> Handle(ReopenLeadCommand command, CancellationToken cancellationToken)
    {
        var leadResult = await leadAccess.LoadAsync(command.LeadId, cancellationToken);

        return leadResult.IsFailure
            ? Result.Failure(leadResult.Error)
            : leadResult.Value.Reopen(currentUser.GetRequiredUserId(), timeProvider.GetUtcNow().UtcDateTime);
    }
}
