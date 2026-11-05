namespace AutoElite.Application.StaffTasks.ReassignStaffTask;

/// <summary>Checks the new assignee and gives them the task.</summary>
internal sealed class ReassignStaffTaskCommandHandler(
    StaffTaskAccess taskAccess,
    TaskAssignees taskAssignees,
    ICurrentUser currentUser,
    TimeProvider timeProvider) : ICommandHandler<ReassignStaffTaskCommand>
{
    /// <inheritdoc />
    public async Task<Result> Handle(ReassignStaffTaskCommand command, CancellationToken cancellationToken)
    {
        var taskResult = await taskAccess.LoadForManagementAsync(command.TaskId, cancellationToken);
        if (taskResult.IsFailure)
            return taskResult;

        var assigneeResult = await taskAssignees.CheckAsync(command.AssigneeId, cancellationToken);
        if (assigneeResult.IsFailure)
            return assigneeResult;

        return taskResult.Value.Reassign(
            command.AssigneeId, currentUser.GetRequiredUserId(), timeProvider.GetUtcNow().UtcDateTime);
    }
}
