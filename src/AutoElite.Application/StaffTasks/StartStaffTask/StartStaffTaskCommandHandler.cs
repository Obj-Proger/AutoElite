namespace AutoElite.Application.StaffTasks.StartStaffTask;

/// <summary>Moves the task to work.</summary>
internal sealed class StartStaffTaskCommandHandler(StaffTaskAccess taskAccess) : ICommandHandler<StartStaffTaskCommand>
{
    /// <inheritdoc />
    public async Task<Result> Handle(StartStaffTaskCommand command, CancellationToken cancellationToken)
    {
        var taskResult = await taskAccess.LoadForWorkAsync(command.TaskId, cancellationToken);

        return taskResult.IsFailure ? taskResult : taskResult.Value.Start();
    }
}
