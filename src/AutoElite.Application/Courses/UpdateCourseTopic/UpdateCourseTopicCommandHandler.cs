namespace AutoElite.Application.Courses.UpdateCourseTopic;

/// <summary>Applies the new title and description to the topic.</summary>
internal sealed class UpdateCourseTopicCommandHandler(ICourseRepository courseRepository)
    : ICommandHandler<UpdateCourseTopicCommand>
{
    /// <inheritdoc />
    public async Task<Result> Handle(UpdateCourseTopicCommand command, CancellationToken cancellationToken)
    {
        var course = await courseRepository.GetByIdAsync(command.CourseId, cancellationToken);

        return course is null
            ? Result.Failure(ApplicationErrors.NotFound<Course>())
            : course.UpdateTopic(command.TopicId, command.Title, command.Description);
    }
}
