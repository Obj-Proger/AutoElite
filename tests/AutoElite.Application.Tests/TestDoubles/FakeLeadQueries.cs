using AutoElite.Application.Common.Authorization;
using AutoElite.Application.Common.Pagination;
using AutoElite.Application.Leads;
using AutoElite.Domain.Entities;
using AutoElite.Domain.Enums;

namespace AutoElite.Application.Tests.TestDoubles;

/// <summary>
/// Returns seeded cards and records the list arguments; the filtering itself is the infrastructure's job. The leads in
/// progress are counted from the leads given, if any.
/// </summary>
/// <param name="leads">The leads to count.</param>
internal sealed class FakeLeadQueries(IEnumerable<Lead>? leads = null) : ILeadQueries
{
    private readonly Dictionary<Guid, LeadDetails> _details = [];

    public (LeadListFilter Filter, DataScope Scope, PageRequest Page)? LastListCall { get; private set; }

    public void Seed(LeadDetails details) => _details.Add(details.Id, details);

    public Task<PagedList<LeadListItem>> GetListAsync(
        LeadListFilter filter, DataScope scope, PageRequest page, CancellationToken cancellationToken = default)
    {
        LastListCall = (filter, scope, page);

        return Task.FromResult(PagedList<LeadListItem>.Empty(page));
    }

    public Task<LeadDetails?> GetDetailsAsync(Guid leadId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_details.GetValueOrDefault(leadId));

    public Task<int> CountInProgressAsync(Guid managerId, CancellationToken cancellationToken = default) =>
        Task.FromResult((leads ?? []).Count(lead => lead.ManagerId == managerId && lead.Status == LeadStatus.InProgress));
}
