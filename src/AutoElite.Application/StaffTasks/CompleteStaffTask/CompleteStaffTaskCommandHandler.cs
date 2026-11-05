namespace AutoElite.Application.StaffTasks.CompleteStaffTask;

/// <summary>Completes the task and saves the next occurrence of a repeating one.</summary>
internal sealed class CompleteStaffTaskCommandHandler(
    StaffTaskAccess taskAccess,
    IRepository<StaffTask> taskRepository,
    ICurrentUser currentUser,
    TimeProvider timeProvider) : ICommandHandler<CompleteStaffTaskCommand>
{
    /// <inheritdoc />
    public async Task<Result> Handle(CompleteStaffTaskCommand command, CancellationToken cancellationToken)
    {
        var taskResult = await taskAccess.LoadForWorkAsync(command.TaskId, cancellationToken);
        if (taskResult.IsFailure)
            return taskResult;

        var nextResult = taskResult.Value.Complete(currentUser.GetRequiredUserId(), timeProvider.GetUtcNow().UtcDateTime);
        if (nextResult.IsFailure)
            return Result.Failure(nextResult.Error);

        if (nextResult.Value is { } next)
            taskRepository.Add(next);

        return Result.Success();
    }
}
