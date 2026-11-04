namespace AutoElite.Application.Courses.MoveCourseTopic;

/// <summary>Moves the topic.</summary>
internal sealed class MoveCourseTopicCommandHandler(ICourseRepository courseRepository) : ICommandHandler<MoveCourseTopicCommand>
{
    /// <inheritdoc />
    public async Task<Result> Handle(MoveCourseTopicCommand command, CancellationToken cancellationToken)
    {
        var course = await courseRepository.GetByIdAsync(command.CourseId, cancellationToken);

        return course is null
            ? Result.Failure(ApplicationErrors.NotFound<Course>())
            : course.MoveTopic(command.TopicId, command.NewPosition);
    }
}
