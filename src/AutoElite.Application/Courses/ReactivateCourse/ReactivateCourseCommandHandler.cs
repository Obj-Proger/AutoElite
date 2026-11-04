namespace AutoElite.Application.Courses.ReactivateCourse;

/// <summary>Reactivates the course.</summary>
internal sealed class ReactivateCourseCommandHandler(ICourseRepository courseRepository) : ICommandHandler<ReactivateCourseCommand>
{
    /// <inheritdoc />
    public async Task<Result> Handle(ReactivateCourseCommand command, CancellationToken cancellationToken)
    {
        var course = await courseRepository.GetByIdAsync(command.CourseId, cancellationToken);

        return course is null ? Result.Failure(ApplicationErrors.NotFound<Course>()) : course.Reactivate();
    }
}
