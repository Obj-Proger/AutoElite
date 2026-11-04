namespace AutoElite.Application.TheoryLessons.CancelTheoryLesson;

/// <summary>Cancels the lesson; the domain checks the status and the reason.</summary>
internal sealed class CancelTheoryLessonCommandHandler(TheoryLessonAccess lessonAccess, TimeProvider timeProvider)
    : ICommandHandler<CancelTheoryLessonCommand>
{
    /// <inheritdoc />
    public async Task<Result> Handle(CancelTheoryLessonCommand command, CancellationToken cancellationToken)
    {
        var loadResult = await lessonAccess.LoadAsync(command.LessonId, cancellationToken);

        return loadResult.IsFailure
            ? Result.Failure(loadResult.Error)
            : loadResult.Value.Lesson.Cancel(command.Reason, timeProvider.GetUtcNow().UtcDateTime);
    }
}
