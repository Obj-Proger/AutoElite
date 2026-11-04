namespace AutoElite.Application.Courses.DeactivateCourse;

/// <summary>Deactivates the course.</summary>
internal sealed class DeactivateCourseCommandHandler(ICourseRepository courseRepository) : ICommandHandler<DeactivateCourseCommand>
{
    /// <inheritdoc />
    public async Task<Result> Handle(DeactivateCourseCommand command, CancellationToken cancellationToken)
    {
        var course = await courseRepository.GetByIdAsync(command.CourseId, cancellationToken);

        return course is null ? Result.Failure(ApplicationErrors.NotFound<Course>()) : course.Deactivate();
    }
}
