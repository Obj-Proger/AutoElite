namespace AutoElite.Application.DrivingSlots.GetMyDrivingLessons;

/// <summary>Checks the period and reads the caller's lessons.</summary>
internal sealed class GetMyDrivingLessonsQueryHandler(IDrivingSlotQueries slotQueries, ICurrentUser currentUser)
    : IQueryHandler<GetMyDrivingLessonsQuery, IReadOnlyList<StudentDrivingLessonItem>>
{
    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<StudentDrivingLessonItem>>> Handle(
        GetMyDrivingLessonsQuery query, CancellationToken cancellationToken)
    {
        var periodResult = SchedulePeriod.Create(query.FromUtc, query.ToUtc);
        if (periodResult.IsFailure)
            return Result.Failure<IReadOnlyList<StudentDrivingLessonItem>>(periodResult.Error);

        return Result.Success(
            await slotQueries.GetOfStudentAsync(currentUser.GetRequiredUserId(), periodResult.Value, cancellationToken));
    }
}
