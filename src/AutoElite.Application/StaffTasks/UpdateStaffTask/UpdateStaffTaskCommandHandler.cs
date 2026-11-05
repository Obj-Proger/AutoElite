namespace AutoElite.Application.StaffTasks.UpdateStaffTask;

/// <summary>Checks the linked record and applies the changes.</summary>
internal sealed class UpdateStaffTaskCommandHandler(
    StaffTaskAccess taskAccess,
    TaskSubjects taskSubjects,
    TimeProvider timeProvider) : ICommandHandler<UpdateStaffTaskCommand>
{
    /// <inheritdoc />
    public async Task<Result> Handle(UpdateStaffTaskCommand command, CancellationToken cancellationToken)
    {
        var taskResult = await taskAccess.LoadForManagementAsync(command.TaskId, cancellationToken);
        if (taskResult.IsFailure)
            return taskResult;

        var subjectResult = await taskSubjects.ResolveAsync(command.SubjectKind, command.SubjectId, cancellationToken);
        if (subjectResult.IsFailure)
            return subjectResult;

        return taskResult.Value.UpdateDetails(
            command.Title, command.Description, command.DueAtUtc, subjectResult.Value, timeProvider.GetUtcNow().UtcDateTime);
    }
}
