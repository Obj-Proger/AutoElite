using AutoElite.Application.Common.Authorization;
using AutoElite.Application.Common.Pagination;
using AutoElite.Application.Leads;

namespace AutoElite.Application.Tests.TestDoubles;

/// <summary>Returns seeded cards and records the list arguments; the filtering itself is the infrastructure's job.</summary>
internal sealed class FakeLeadQueries : ILeadQueries
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
}
