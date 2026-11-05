using AutoElite.Application.Users;

namespace AutoElite.Application.StaffTasks;

/// <summary>
/// Loads a task for an action of the caller. Tasks have no branch of their own: a task belongs to the branch of its
/// assignee, so a manager manages the tasks of the staff inside the manager's branch scope and an administrator
/// manages every task. An assignee may always work on their own task. Any other task is reported as not found.
/// </summary>
internal sealed class StaffTaskAccess(
    IRepository<StaffTask> taskRepository,
    IUserRepository userRepository,
    ICurrentUser currentUser)
{
    /// <summary>Loads the task for editing, reassigning and the like, which only managers and administrators do.</summary>
    /// <param name="taskId">The identifier of the task.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    public async Task<Result<StaffTask>> LoadForManagementAsync(Guid taskId, CancellationToken cancellationToken)
    {
        var task = await taskRepository.GetByIdAsync(taskId, cancellationToken);

        return task is not null && await CanManageAsync(task, cancellationToken)
            ? task
            : Result.Failure<StaffTask>(ApplicationErrors.NotFound<StaffTask>());
    }

    /// <summary>Loads the task for work on it: starting, completing or reopening.</summary>
    /// <param name="taskId">The identifier of the task.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    public async Task<Result<StaffTask>> LoadForWorkAsync(Guid taskId, CancellationToken cancellationToken)
    {
        var task = await taskRepository.GetByIdAsync(taskId, cancellationToken);

        return task is not null && (task.AssigneeId == currentUser.UserId || await CanManageAsync(task, cancellationToken))
            ? task
            : Result.Failure<StaffTask>(ApplicationErrors.NotFound<StaffTask>());
    }

    private async Task<bool> CanManageAsync(StaffTask task, CancellationToken cancellationToken)
    {
        if (currentUser.IsInRole(UserRole.Administrator))
            return true;

        if (!currentUser.IsInRole(UserRole.Manager))
            return false;

        var assignee = await userRepository.GetByIdAsync(task.AssigneeId, cancellationToken);

        return assignee is not null && DataScope.For(currentUser).Includes(assignee.BranchId);
    }
}
