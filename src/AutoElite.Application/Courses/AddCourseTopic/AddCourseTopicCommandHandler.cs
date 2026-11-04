namespace AutoElite.Application.Courses.AddCourseTopic;

/// <summary>Adds the topic and returns its identifier.</summary>
internal sealed class AddCourseTopicCommandHandler(ICourseRepository courseRepository)
    : ICommandHandler<AddCourseTopicCommand, Guid>
{
    /// <inheritdoc />
    public async Task<Result<Guid>> Handle(AddCourseTopicCommand command, CancellationToken cancellationToken)
    {
        var course = await courseRepository.GetByIdAsync(command.CourseId, cancellationToken);
        if (course is null)
            return Result.Failure<Guid>(ApplicationErrors.NotFound<Course>());

        var topicResult = course.AddTopic(command.Title, command.Description);

        return topicResult.IsFailure ? Result.Failure<Guid>(topicResult.Error) : topicResult.Value.Id;
    }
}
