namespace AutoElite.Application.StudyGroups.ReactivateStudyGroup;

/// <summary>Reactivates the group.</summary>
internal sealed class ReactivateStudyGroupCommandHandler(StudyGroupAccess groupAccess) : ICommandHandler<ReactivateStudyGroupCommand>
{
    /// <inheritdoc />
    public async Task<Result> Handle(ReactivateStudyGroupCommand command, CancellationToken cancellationToken)
    {
        var groupResult = await groupAccess.LoadAsync(command.GroupId, cancellationToken);

        return groupResult.IsFailure ? Result.Failure(groupResult.Error) : groupResult.Value.Reactivate();
    }
}
