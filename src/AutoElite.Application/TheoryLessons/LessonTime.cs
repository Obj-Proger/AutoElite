namespace AutoElite.Application.TheoryLessons;

/// <summary>Checks the time of a lesson against the other lessons of its group and instructor.</summary>
internal static class LessonTime
{
    /// <summary>Creates the interval and rejects a time when the group or the instructor already has a lesson.</summary>
    /// <param name="lessonRepository">The lesson repository.</param>
    /// <param name="groupId">The group of the lesson.</param>
    /// <param name="instructorId">The instructor of the lesson.</param>
    /// <param name="startUtc">The start in UTC.</param>
    /// <param name="endUtc">The end in UTC.</param>
    /// <param name="exceptLessonId">The lesson being moved, if any.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    public static async Task<Result<TimeInterval>> CheckAsync(
        ITheoryLessonRepository lessonRepository,
        Guid groupId,
        Guid instructorId,
        DateTime startUtc,
        DateTime endUtc,
        Guid? exceptLessonId,
        CancellationToken cancellationToken)
    {
        var timeResult = TimeInterval.Create(startUtc, endUtc);
        if (timeResult.IsFailure)
            return timeResult;

        return await lessonRepository.HasOverlapAsync(groupId, instructorId, timeResult.Value, exceptLessonId, cancellationToken)
            ? Result.Failure<TimeInterval>(ApplicationErrors.TheoryLesson.TimeConflict)
            : timeResult;
    }
}
