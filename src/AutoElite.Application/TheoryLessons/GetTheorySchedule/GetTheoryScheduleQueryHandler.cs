namespace AutoElite.Application.TheoryLessons.GetTheorySchedule;

/// <summary>
/// Reads the schedule within the caller's scope. A theory instructor who is not also staff of the office reads only
/// their own lessons; asking for someone else's lessons or a branch outside the scope gives an empty schedule.
/// </summary>
internal sealed class GetTheoryScheduleQueryHandler(ITheoryLessonQueries lessonQueries, ICurrentUser currentUser)
    : IQueryHandler<GetTheoryScheduleQuery, IReadOnlyList<TheoryLessonItem>>
{
    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<TheoryLessonItem>>> Handle(GetTheoryScheduleQuery query, CancellationToken cancellationToken)
    {
        var periodResult = SchedulePeriod.Create(query.FromUtc, query.ToUtc);
        if (periodResult.IsFailure)
            return Result.Failure<IReadOnlyList<TheoryLessonItem>>(periodResult.Error);

        var scope = DataScope.For(currentUser);

        var instructorId = query.InstructorId;
        if (!currentUser.IsInAnyRole(UserRole.Administrator, UserRole.Manager))
        {
            var ownId = currentUser.GetRequiredUserId();
            if (instructorId is { } requested && requested != ownId)
                return Result.Success<IReadOnlyList<TheoryLessonItem>>([]);

            instructorId = ownId;
        }

        if (query.BranchId is { } branchId && !scope.Includes(branchId))
            return Result.Success<IReadOnlyList<TheoryLessonItem>>([]);

        var filter = new TheoryScheduleFilter(periodResult.Value, instructorId, query.GroupId, query.BranchId, query.Status);

        return Result.Success(await lessonQueries.GetScheduleAsync(filter, scope, cancellationToken));
    }
}
