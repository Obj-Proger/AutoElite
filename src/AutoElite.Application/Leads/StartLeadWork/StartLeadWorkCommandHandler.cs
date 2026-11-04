namespace AutoElite.Application.Leads.StartLeadWork;

/// <summary>Moves the lead to the in-progress status on behalf of the caller.</summary>
internal sealed class StartLeadWorkCommandHandler(LeadAccess leadAccess, ICurrentUser currentUser, TimeProvider timeProvider)
    : ICommandHandler<StartLeadWorkCommand>
{
    /// <inheritdoc />
    public async Task<Result> Handle(StartLeadWorkCommand command, CancellationToken cancellationToken)
    {
        var leadResult = await leadAccess.LoadAsync(command.LeadId, cancellationToken);

        return leadResult.IsFailure
            ? Result.Failure(leadResult.Error)
            : leadResult.Value.StartWork(currentUser.GetRequiredUserId(), timeProvider.GetUtcNow().UtcDateTime);
    }
}
