namespace AutoElite.Application.StudyGroups.UpdateStudyGroup;

/// <summary>Checks a new instructor and applies the details.</summary>
internal sealed class UpdateStudyGroupCommandHandler(StudyGroupAccess groupAccess, GroupInstructors groupInstructors)
    : ICommandHandler<UpdateStudyGroupCommand>
{
    /// <inheritdoc />
    public async Task<Result> Handle(UpdateStudyGroupCommand command, CancellationToken cancellationToken)
    {
        var groupResult = await groupAccess.LoadAsync(command.GroupId, cancellationToken);
        if (groupResult.IsFailure)
            return Result.Failure(groupResult.Error);

        var group = groupResult.Value;
        if (command.TheoryInstructorId != group.TheoryInstructorId)
        {
            var instructorResult = await groupInstructors.CheckAsync(command.TheoryInstructorId, group.BranchId, cancellationToken);
            if (instructorResult.IsFailure)
                return instructorResult;
        }

        return group.UpdateDetails(command.Name, command.TheoryInstructorId, command.StartDate, command.EndDate, command.Capacity);
    }
}
