namespace AutoElite.Application.TheoryLessons.GetGroupTheoryLessons;

/// <summary>Reads the lessons and hides groups the caller may not see as not found.</summary>
internal sealed class GetGroupTheoryLessonsQueryHandler(ITheoryLessonQueries lessonQueries, ICurrentUser currentUser)
    : IQueryHandler<GetGroupTheoryLessonsQuery, IReadOnlyList<TheoryLessonItem>>
{
    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<TheoryLessonItem>>> Handle(
        GetGroupTheoryLessonsQuery query, CancellationToken cancellationToken)
    {
        var group = await lessonQueries.GetOfGroupAsync(query.GroupId, cancellationToken);
        if (group is null || !MayRead(group))
            return Result.Failure<IReadOnlyList<TheoryLessonItem>>(ApplicationErrors.NotFound<StudyGroup>());

        return Result.Success(group.Lessons);
    }

    private bool MayRead(GroupTheoryLessons group)
    {
        if (currentUser.IsInAnyRole(UserRole.Administrator, UserRole.Manager) && DataScope.For(currentUser).Includes(group.BranchId))
            return true;

        return group.TheoryInstructorId == currentUser.UserId;
    }
}
