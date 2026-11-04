namespace AutoElite.Application.StudyGroups.DeactivateStudyGroup;

/// <summary>Deactivates the group.</summary>
internal sealed class DeactivateStudyGroupCommandHandler(StudyGroupAccess groupAccess) : ICommandHandler<DeactivateStudyGroupCommand>
{
    /// <inheritdoc />
    public async Task<Result> Handle(DeactivateStudyGroupCommand command, CancellationToken cancellationToken)
    {
        var groupResult = await groupAccess.LoadAsync(command.GroupId, cancellationToken);

        return groupResult.IsFailure ? Result.Failure(groupResult.Error) : groupResult.Value.Deactivate();
    }
}
