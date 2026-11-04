namespace AutoElite.Application.Leads.GetLead;

/// <summary>Reads the card and hides leads outside the caller's scope as not found.</summary>
internal sealed class GetLeadQueryHandler(ILeadQueries leadQueries, ICurrentUser currentUser) : IQueryHandler<GetLeadQuery, LeadDetails>
{
    /// <inheritdoc />
    public async Task<Result<LeadDetails>> Handle(GetLeadQuery query, CancellationToken cancellationToken)
    {
        var details = await leadQueries.GetDetailsAsync(query.LeadId, cancellationToken);
        if (details is null || !DataScope.For(currentUser).Includes(details.BranchId))
            return Result.Failure<LeadDetails>(ApplicationErrors.NotFound<Lead>());

        return details;
    }
}
