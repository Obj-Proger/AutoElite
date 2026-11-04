namespace AutoElite.Application.TheoryLessons.RescheduleTheoryLesson;

/// <summary>Checks the new time against the other lessons and moves the lesson.</summary>
internal sealed class RescheduleTheoryLessonCommandHandler(
    TheoryLessonAccess lessonAccess,
    ITheoryLessonRepository lessonRepository,
    TimeProvider timeProvider) : ICommandHandler<RescheduleTheoryLessonCommand>
{
    /// <inheritdoc />
    public async Task<Result> Handle(RescheduleTheoryLessonCommand command, CancellationToken cancellationToken)
    {
        var loadResult = await lessonAccess.LoadAsync(command.LessonId, cancellationToken);
        if (loadResult.IsFailure)
            return Result.Failure(loadResult.Error);

        var lesson = loadResult.Value.Lesson;
        var timeResult = await LessonTime.CheckAsync(
            lessonRepository, lesson.GroupId, lesson.InstructorId, command.StartUtc, command.EndUtc, lesson.Id, cancellationToken);
        if (timeResult.IsFailure)
            return Result.Failure(timeResult.Error);

        return lesson.Reschedule(timeResult.Value, timeProvider.GetUtcNow().UtcDateTime);
    }
}
