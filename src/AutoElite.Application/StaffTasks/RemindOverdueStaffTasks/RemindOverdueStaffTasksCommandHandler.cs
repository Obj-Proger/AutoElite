namespace AutoElite.Application.StaffTasks.RemindOverdueStaffTasks;

/// <summary>
/// Marks a batch of overdue tasks. The batch is bounded so that one run stays short after a long pause; the rest is
/// marked by the next runs.
/// </summary>
internal sealed class RemindOverdueStaffTasksCommandHandler(IStaffTaskRepository taskRepository, TimeProvider timeProvider)
    : ICommandHandler<RemindOverdueStaffTasksCommand>
{
    /// <summary>The most tasks marked in one run.</summary>
    internal const int BatchSize = 200;

    /// <inheritdoc />
    public async Task<Result> Handle(RemindOverdueStaffTasksCommand command, CancellationToken cancellationToken)
    {
        var utcNow = timeProvider.GetUtcNow().UtcDateTime;
        var tasks = await taskRepository.GetOverdueToRemindAsync(utcNow, BatchSize, cancellationToken);

        foreach (var task in tasks)
            task.MarkOverdue(utcNow);

        return Result.Success();
    }
}
