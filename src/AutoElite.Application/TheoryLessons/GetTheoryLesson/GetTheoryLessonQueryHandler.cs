namespace AutoElite.Application.TheoryLessons.GetTheoryLesson;

/// <summary>Reads the card and hides lessons the caller may not see as not found.</summary>
internal sealed class GetTheoryLessonQueryHandler(ITheoryLessonQueries lessonQueries, ICurrentUser currentUser)
    : IQueryHandler<GetTheoryLessonQuery, TheoryLessonDetails>
{
    /// <inheritdoc />
    public async Task<Result<TheoryLessonDetails>> Handle(GetTheoryLessonQuery query, CancellationToken cancellationToken)
    {
        var details = await lessonQueries.GetDetailsAsync(query.LessonId, cancellationToken);

        return details is not null && MayRead(details)
            ? details
            : Result.Failure<TheoryLessonDetails>(ApplicationErrors.NotFound<TheoryLesson>());
    }

    private bool MayRead(TheoryLessonDetails details)
    {
        if (currentUser.IsInAnyRole(UserRole.Administrator, UserRole.Manager) && DataScope.For(currentUser).Includes(details.BranchId))
            return true;

        return details.InstructorId == currentUser.UserId || details.ResponsibleInstructorId == currentUser.UserId;
    }
}
