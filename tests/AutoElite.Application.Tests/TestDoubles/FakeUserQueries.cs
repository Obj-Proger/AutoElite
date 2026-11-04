using AutoElite.Application.Common.Authorization;
using AutoElite.Application.Common.Pagination;
using AutoElite.Application.Users;

namespace AutoElite.Application.Tests.TestDoubles;

/// <summary>Returns seeded results and records the list arguments; the filtering itself is the infrastructure's job.</summary>
internal sealed class FakeUserQueries : IUserQueries
{
    private readonly Dictionary<Guid, UserProfile> _profiles = [];
    private readonly Dictionary<Guid, UserDetails> _details = [];

    public (UserListFilter Filter, DataScope Scope, PageRequest Page)? LastListCall { get; private set; }

    public IReadOnlyList<UserListItem> ListItems { get; set; } = [];

    public void Seed(UserProfile profile) => _profiles.Add(profile.Id, profile);

    public void Seed(UserDetails details) => _details.Add(details.Id, details);

    public Task<UserProfile?> GetProfileAsync(Guid userId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_profiles.GetValueOrDefault(userId));

    public Task<UserDetails?> GetDetailsAsync(Guid userId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_details.GetValueOrDefault(userId));

    public Task<PagedList<UserListItem>> GetListAsync(
        UserListFilter filter, DataScope scope, PageRequest page, CancellationToken cancellationToken = default)
    {
        LastListCall = (filter, scope, page);

        return Task.FromResult(new PagedList<UserListItem>(ListItems, page, ListItems.Count));
    }
}
