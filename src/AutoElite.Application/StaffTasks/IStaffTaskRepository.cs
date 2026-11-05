namespace AutoElite.Application.StaffTasks;

/// <summary>Loads and adds staff tasks for commands.</summary>
public interface IStaffTaskRepository : IRepository<StaffTask>
{
    /// <summary>
    /// Loads open tasks past their deadline whose assignee has not been reminded for the current deadline, the
    /// earliest deadline first.
    /// </summary>
    /// <param name="utcNow">The current UTC time.</param>
    /// <param name="limit">The most tasks to load; the rest wait for the next run.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    Task<IReadOnlyList<StaffTask>> GetOverdueToRemindAsync(DateTime utcNow, int limit, CancellationToken cancellationToken = default);
}
