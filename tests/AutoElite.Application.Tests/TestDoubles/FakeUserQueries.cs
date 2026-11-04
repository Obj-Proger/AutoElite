using AutoElite.Application.Users;

namespace AutoElite.Application.Tests.TestDoubles;

internal sealed class FakeUserQueries : IUserQueries
{
    private readonly Dictionary<Guid, UserProfile> _profiles = [];

    public void Seed(UserProfile profile) => _profiles.Add(profile.Id, profile);

    public Task<UserProfile?> GetProfileAsync(Guid userId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_profiles.GetValueOrDefault(userId));
}
