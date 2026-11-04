namespace AutoElite.Application.StudyGroups.GetStudyGroup;

/// <summary>Reads the card and hides groups the caller may not see as not found.</summary>
internal sealed class GetStudyGroupQueryHandler(IStudyGroupQueries groupQueries, ICurrentUser currentUser)
    : IQueryHandler<GetStudyGroupQuery, StudyGroupDetails>
{
    /// <inheritdoc />
    public async Task<Result<StudyGroupDetails>> Handle(GetStudyGroupQuery query, CancellationToken cancellationToken)
    {
        var details = await groupQueries.GetDetailsAsync(query.GroupId, cancellationToken);

        return details is not null && MayRead(details)
            ? details
            : Result.Failure<StudyGroupDetails>(ApplicationErrors.NotFound<StudyGroup>());
    }

    private bool MayRead(StudyGroupDetails details)
    {
        if (currentUser.IsInAnyRole(UserRole.Administrator, UserRole.Manager) && DataScope.For(currentUser).Includes(details.BranchId))
            return true;

        return details.TheoryInstructorId == currentUser.UserId;
    }
}
