namespace AutoElite.Application.Leads.MarkLeadLost;

/// <summary>Marks the lead lost on behalf of the caller; the domain checks the reason.</summary>
internal sealed class MarkLeadLostCommandHandler(LeadAccess leadAccess, ICurrentUser currentUser, TimeProvider timeProvider)
    : ICommandHandler<MarkLeadLostCommand>
{
    /// <inheritdoc />
    public async Task<Result> Handle(MarkLeadLostCommand command, CancellationToken cancellationToken)
    {
        var leadResult = await leadAccess.LoadAsync(command.LeadId, cancellationToken);

        return leadResult.IsFailure
            ? Result.Failure(leadResult.Error)
            : leadResult.Value.MarkLost(command.Reason, currentUser.GetRequiredUserId(), timeProvider.GetUtcNow().UtcDateTime);
    }
}
