namespace AutoElite.Application.TheoryLessons.ChangeTheoryLessonInstructor;

/// <summary>Checks the substitute and the substitute's time, then changes the instructor.</summary>
internal sealed class ChangeTheoryLessonInstructorCommandHandler(
    TheoryLessonAccess lessonAccess,
    ITheoryLessonRepository lessonRepository,
    LessonInstructors lessonInstructors) : ICommandHandler<ChangeTheoryLessonInstructorCommand>
{
    /// <inheritdoc />
    public async Task<Result> Handle(ChangeTheoryLessonInstructorCommand command, CancellationToken cancellationToken)
    {
        var loadResult = await lessonAccess.LoadAsync(command.LessonId, cancellationToken);
        if (loadResult.IsFailure)
            return Result.Failure(loadResult.Error);

        var (lesson, group) = loadResult.Value;
        var instructorResult = await lessonInstructors.CheckAsync(command.InstructorId, group, cancellationToken);
        if (instructorResult.IsFailure)
            return instructorResult;

        if (await lessonRepository.HasOverlapAsync(group.Id, command.InstructorId, lesson.Time, lesson.Id, cancellationToken))
            return Result.Failure(ApplicationErrors.TheoryLesson.TimeConflict);

        return lesson.ChangeInstructor(command.InstructorId);
    }
}
