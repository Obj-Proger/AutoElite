using AutoElite.Application.Notifications;
using AutoElite.Domain.Entities;

namespace AutoElite.Application.Tests.TestDoubles;

internal sealed class FakePushDeviceRepository : InMemoryRepository<PushDevice>, IPushDeviceRepository
{
    public Task<PushDevice?> FindByTokenAsync(string token, CancellationToken cancellationToken = default) =>
        Task.FromResult(Items.SingleOrDefault(device => device.Token == token));

    public Task<IReadOnlyList<PushDevice>> ListOfUserAsync(Guid userId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<PushDevice>>([.. Items.Where(device => device.UserId == userId)]);
}
