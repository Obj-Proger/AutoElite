using AutoElite.Application.StaffTasks;
using AutoElite.Domain.Enums;

namespace AutoElite.Infrastructure.Persistence.Repositories;

/// <summary>Loads and adds staff tasks; a task has no child entities.</summary>
internal sealed class StaffTaskRepository(AutoEliteDbContext dbContext)
    : Repository<StaffTask>(dbContext), IStaffTaskRepository
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<StaffTask>> GetOverdueToRemindAsync(
        DateTime utcNow, int limit, CancellationToken cancellationToken = default) =>
        await Aggregates
            .Where(task => task.Status != StaffTaskStatus.Done && task.DueAtUtc < utcNow && task.OverdueNotifiedAtUtc == null)
            .OrderBy(task => task.DueAtUtc)
            .ThenBy(task => task.Id)
            .Take(limit)
            .ToListAsync(cancellationToken);
}
