using AutoElite.Application.Users;
using AutoElite.Domain.Entities;

namespace AutoElite.Application.Tests.TestDoubles;

internal sealed class FakeUserRepository : InMemoryRepository<User>, IUserRepository
{
    public Task<bool> EmailExistsAsync(Email email, CancellationToken cancellationToken = default) =>
        Task.FromResult(Items.Any(user => user.Email == email));

    public Task<User?> GetByEmailAsync(Email email, CancellationToken cancellationToken = default) =>
        Task.FromResult(Items.SingleOrDefault(user => user.Email == email));

    public Task<IReadOnlyList<User>> GetByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<User>>([.. Items.Where(user => ids.Contains(user.Id))]);
}
