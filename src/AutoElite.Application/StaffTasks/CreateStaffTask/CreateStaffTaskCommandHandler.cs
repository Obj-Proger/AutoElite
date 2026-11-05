namespace AutoElite.Application.StaffTasks.CreateStaffTask;

/// <summary>Checks the assignee and the linked record, then creates the task.</summary>
internal sealed class CreateStaffTaskCommandHandler(
    IRepository<StaffTask> taskRepository,
    TaskAssignees taskAssignees,
    TaskSubjects taskSubjects,
    ICurrentUser currentUser,
    TimeProvider timeProvider) : ICommandHandler<CreateStaffTaskCommand, Guid>
{
    /// <inheritdoc />
    public async Task<Result<Guid>> Handle(CreateStaffTaskCommand command, CancellationToken cancellationToken)
    {
        var assigneeResult = await taskAssignees.CheckAsync(command.AssigneeId, cancellationToken);
        if (assigneeResult.IsFailure)
            return Result.Failure<Guid>(assigneeResult.Error);

        var subjectResult = await taskSubjects.ResolveAsync(command.SubjectKind, command.SubjectId, cancellationToken);
        if (subjectResult.IsFailure)
            return Result.Failure<Guid>(subjectResult.Error);

        var taskResult = StaffTask.Create(
            command.Title,
            command.Description,
            command.AssigneeId,
            command.DueAtUtc,
            subjectResult.Value,
            command.Recurrence,
            currentUser.GetRequiredUserId(),
            timeProvider.GetUtcNow().UtcDateTime);
        if (taskResult.IsFailure)
            return Result.Failure<Guid>(taskResult.Error);

        taskRepository.Add(taskResult.Value);

        return taskResult.Value.Id;
    }
}
