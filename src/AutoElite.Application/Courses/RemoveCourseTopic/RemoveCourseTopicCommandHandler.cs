namespace AutoElite.Application.Courses.RemoveCourseTopic;

/// <summary>Checks that nothing refers to the topic and removes it.</summary>
internal sealed class RemoveCourseTopicCommandHandler(ICourseRepository courseRepository)
    : ICommandHandler<RemoveCourseTopicCommand>
{
    /// <inheritdoc />
    public async Task<Result> Handle(RemoveCourseTopicCommand command, CancellationToken cancellationToken)
    {
        var course = await courseRepository.GetByIdAsync(command.CourseId, cancellationToken);
        if (course is null)
            return Result.Failure(ApplicationErrors.NotFound<Course>());

        if (course.Topics.Any(topic => topic.Id == command.TopicId)
            && await courseRepository.IsTopicInUseAsync(command.TopicId, cancellationToken))
        {
            return Result.Failure(ApplicationErrors.Course.TopicInUse);
        }

        return course.RemoveTopic(command.TopicId);
    }
}
