namespace AutoElite.Application.Leads;

/// <summary>Loads a lead for a staff action, hiding leads outside the caller's branch scope as not found.</summary>
internal sealed class LeadAccess(IRepository<Lead> leadRepository, ICurrentUser currentUser)
{
    /// <summary>Loads the lead if it is inside the caller's scope.</summary>
    /// <param name="leadId">The identifier of the lead.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    public async Task<Result<Lead>> LoadAsync(Guid leadId, CancellationToken cancellationToken)
    {
        var lead = await leadRepository.GetByIdAsync(leadId, cancellationToken);

        return lead is not null && DataScope.For(currentUser).Includes(lead.BranchId)
            ? lead
            : Result.Failure<Lead>(ApplicationErrors.NotFound<Lead>());
    }
}
