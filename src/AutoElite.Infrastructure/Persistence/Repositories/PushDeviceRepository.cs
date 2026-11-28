using AutoElite.Application.Notifications;

namespace AutoElite.Infrastructure.Persistence.Repositories;

/// <summary>Loads, adds and removes the devices that receive push notifications.</summary>
internal sealed class PushDeviceRepository(AutoEliteDbContext dbContext)
    : Repository<PushDevice>(dbContext), IPushDeviceRepository
{
    /// <inheritdoc />
    public Task<PushDevice?> FindByTokenAsync(string token, CancellationToken cancellationToken = default) =>
        Aggregates.FirstOrDefaultAsync(device => device.Token == token, cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<PushDevice>> ListOfUserAsync(Guid userId, CancellationToken cancellationToken = default) =>
        await Aggregates.Where(device => device.UserId == userId).OrderBy(device => device.Id).ToListAsync(cancellationToken);

    /// <inheritdoc />
    public void Remove(PushDevice device) => Aggregates.Remove(device);
}
