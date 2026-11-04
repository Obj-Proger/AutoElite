using AutoElite.Application.Common.Authorization;
using AutoElite.Application.Common.Pagination;
using AutoElite.Application.Contracts;

namespace AutoElite.Application.Tests.TestDoubles;

/// <summary>Returns seeded rows and cards and records the list arguments; filtering is the infrastructure's job.</summary>
internal sealed class FakeContractQueries : IContractQueries
{
    private readonly Dictionary<Guid, ContractDetails> _details = [];

    public List<ContractListItem> Items { get; } = [];

    public (ContractListFilter Filter, DataScope Scope, PageRequest Page)? LastListCall { get; private set; }

    public void Seed(ContractDetails details) => _details.Add(details.Id, details);

    public Task<PagedList<ContractListItem>> GetListAsync(
        ContractListFilter filter, DataScope scope, PageRequest page, CancellationToken cancellationToken = default)
    {
        LastListCall = (filter, scope, page);

        return Task.FromResult(PagedList<ContractListItem>.Empty(page));
    }

    public Task<IReadOnlyList<ContractListItem>> GetOfStudentAsync(Guid studentId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<ContractListItem>>([.. Items.Where(item => item.StudentId == studentId)]);

    public Task<ContractDetails?> GetDetailsAsync(Guid contractId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_details.GetValueOrDefault(contractId));
}
