namespace AutoElite.Application.TheoryLessons.GetMyTheorySchedule;

/// <summary>Checks the period and reads the caller's lessons.</summary>
internal sealed class GetMyTheoryScheduleQueryHandler(ITheoryLessonQueries lessonQueries, ICurrentUser currentUser)
    : IQueryHandler<GetMyTheoryScheduleQuery, IReadOnlyList<StudentTheoryLessonItem>>
{
    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<StudentTheoryLessonItem>>> Handle(
        GetMyTheoryScheduleQuery query, CancellationToken cancellationToken)
    {
        var periodResult = SchedulePeriod.Create(query.FromUtc, query.ToUtc);
        if (periodResult.IsFailure)
            return Result.Failure<IReadOnlyList<StudentTheoryLessonItem>>(periodResult.Error);

        return Result.Success(
            await lessonQueries.GetOfStudentAsync(currentUser.GetRequiredUserId(), periodResult.Value, cancellationToken));
    }
}
