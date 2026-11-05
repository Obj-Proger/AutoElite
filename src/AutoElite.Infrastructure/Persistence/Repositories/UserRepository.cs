using AutoElite.Application.Users;

namespace AutoElite.Infrastructure.Persistence.Repositories;

/// <summary>Loads and adds users.</summary>
internal sealed class UserRepository(AutoEliteDbContext dbContext) : Repository<User>(dbContext), IUserRepository
{
    /// <inheritdoc />
    public Task<bool> EmailExistsAsync(Email email, CancellationToken cancellationToken = default) =>
        Aggregates.AnyAsync(user => user.Email == email, cancellationToken);

    /// <inheritdoc />
    public Task<User?> GetByEmailAsync(Email email, CancellationToken cancellationToken = default) =>
        Aggregates.FirstOrDefaultAsync(user => user.Email == email, cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<User>> GetByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken = default) =>
        ids.Count == 0 ? [] : await Aggregates.Where(user => ids.Contains(user.Id)).ToListAsync(cancellationToken);
}
