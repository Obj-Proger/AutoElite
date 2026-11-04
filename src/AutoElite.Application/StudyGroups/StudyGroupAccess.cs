namespace AutoElite.Application.StudyGroups;

/// <summary>Loads a study group for a staff action, hiding groups outside the caller's branch scope as not found.</summary>
internal sealed class StudyGroupAccess(IStudyGroupRepository groupRepository, ICurrentUser currentUser)
{
    /// <summary>Loads the group if it is inside the caller's scope.</summary>
    /// <param name="groupId">The identifier of the group.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    public async Task<Result<StudyGroup>> LoadAsync(Guid groupId, CancellationToken cancellationToken)
    {
        var group = await groupRepository.GetByIdAsync(groupId, cancellationToken);

        return group is not null && DataScope.For(currentUser).Includes(group.BranchId)
            ? group
            : Result.Failure<StudyGroup>(ApplicationErrors.NotFound<StudyGroup>());
    }
}
