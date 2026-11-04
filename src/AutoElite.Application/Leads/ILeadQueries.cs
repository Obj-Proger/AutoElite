using AutoElite.Application.Common.Pagination;

namespace AutoElite.Application.Leads;

/// <summary>Reads leads for queries.</summary>
public interface ILeadQueries
{
    /// <summary>Reads one page of the leads that match the filter and are inside the scope, newest first.</summary>
    /// <param name="filter">The conditions of the list.</param>
    /// <param name="scope">The branches the caller may see; leads without a branch are in every scope.</param>
    /// <param name="page">The page to read.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    Task<PagedList<LeadListItem>> GetListAsync(
        LeadListFilter filter, DataScope scope, PageRequest page, CancellationToken cancellationToken = default);

    /// <summary>Reads the card of a lead.</summary>
    /// <param name="leadId">The identifier of the lead.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    /// <returns>The card, or <see langword="null"/> if the lead does not exist.</returns>
    Task<LeadDetails?> GetDetailsAsync(Guid leadId, CancellationToken cancellationToken = default);
}
