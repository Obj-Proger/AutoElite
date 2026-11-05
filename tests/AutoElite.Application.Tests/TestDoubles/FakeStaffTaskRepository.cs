using AutoElite.Application.StaffTasks;
using AutoElite.Domain.Entities;
using AutoElite.Domain.Enums;

namespace AutoElite.Application.Tests.TestDoubles;

internal sealed class FakeStaffTaskRepository : InMemoryRepository<StaffTask>, IStaffTaskRepository
{
    public Task<IReadOnlyList<StaffTask>> GetOverdueToRemindAsync(
        DateTime utcNow, int limit, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<StaffTask>>([
            .. Items
                .Where(task => task.Status != StaffTaskStatus.Done && task.DueAtUtc < utcNow && task.OverdueNotifiedAtUtc is null)
                .OrderBy(task => task.DueAtUtc)
                .Take(limit)
        ]);
}
