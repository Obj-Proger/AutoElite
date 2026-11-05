using AutoElite.Application.Users;

namespace AutoElite.Application.Notifications.StaffTasks;

/// <summary>Writes the deadline of a task in the local time of its assignee's branch.</summary>
internal sealed class StaffTaskNotices(IUserRepository userRepository, LocalTimes localTimes)
{
    /// <summary>Converts the deadline to the local time of the assignee.</summary>
    /// <param name="task">The task.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    public async Task<DateTime> LocalDeadlineAsync(StaffTask task, CancellationToken cancellationToken)
    {
        var assignee = await userRepository.GetByIdAsync(task.AssigneeId, cancellationToken);

        return await localTimes.ToLocalAsync(task.DueAtUtc, assignee?.BranchId, cancellationToken);
    }
}
