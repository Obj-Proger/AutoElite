using AutoElite.Application.Common.Authorization;
using AutoElite.Application.Common.Pagination;
using AutoElite.Application.StaffTasks;

namespace AutoElite.Application.Tests.TestDoubles;

/// <summary>Returns seeded cards and records the list arguments; the filtering itself is the infrastructure's job.</summary>
internal sealed class FakeStaffTaskQueries : IStaffTaskQueries
{
    private readonly Dictionary<Guid, StaffTaskDetails> _details = [];

    public (StaffTaskListFilter Filter, DataScope Scope, PageRequest Page)? LastListCall { get; private set; }

    public void Seed(StaffTaskDetails details) => _details.Add(details.Id, details);

    public Task<PagedList<StaffTaskListItem>> GetListAsync(
        StaffTaskListFilter filter, DataScope scope, PageRequest page, CancellationToken cancellationToken = default)
    {
        LastListCall = (filter, scope, page);

        return Task.FromResult(PagedList<StaffTaskListItem>.Empty(page));
    }

    public Task<StaffTaskDetails?> GetDetailsAsync(Guid taskId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_details.GetValueOrDefault(taskId));
}
