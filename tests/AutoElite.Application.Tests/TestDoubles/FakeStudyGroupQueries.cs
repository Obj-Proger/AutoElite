using AutoElite.Application.Common.Authorization;
using AutoElite.Application.Common.Pagination;
using AutoElite.Application.StudyGroups;

namespace AutoElite.Application.Tests.TestDoubles;

/// <summary>Returns seeded cards and records the list arguments; filtering is the infrastructure's job.</summary>
internal sealed class FakeStudyGroupQueries : IStudyGroupQueries
{
    private readonly Dictionary<Guid, StudyGroupDetails> _details = [];

    public (StudyGroupListFilter Filter, DataScope Scope, PageRequest Page)? LastListCall { get; private set; }

    public void Seed(StudyGroupDetails details) => _details.Add(details.Id, details);

    public Task<PagedList<StudyGroupListItem>> GetListAsync(
        StudyGroupListFilter filter, DataScope scope, PageRequest page, CancellationToken cancellationToken = default)
    {
        LastListCall = (filter, scope, page);

        return Task.FromResult(PagedList<StudyGroupListItem>.Empty(page));
    }

    public Task<StudyGroupDetails?> GetDetailsAsync(Guid groupId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_details.GetValueOrDefault(groupId));
}
