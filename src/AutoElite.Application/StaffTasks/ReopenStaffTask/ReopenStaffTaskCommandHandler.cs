namespace AutoElite.Application.StaffTasks.ReopenStaffTask;

/// <summary>Reopens the task.</summary>
internal sealed class ReopenStaffTaskCommandHandler(StaffTaskAccess taskAccess) : ICommandHandler<ReopenStaffTaskCommand>
{
    /// <inheritdoc />
    public async Task<Result> Handle(ReopenStaffTaskCommand command, CancellationToken cancellationToken)
    {
        var taskResult = await taskAccess.LoadForWorkAsync(command.TaskId, cancellationToken);

        return taskResult.IsFailure ? taskResult : taskResult.Value.Reopen();
    }
}
