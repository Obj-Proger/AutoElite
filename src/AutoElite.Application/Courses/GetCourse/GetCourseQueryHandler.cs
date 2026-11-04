namespace AutoElite.Application.Courses.GetCourse;

/// <summary>Reads the course.</summary>
internal sealed class GetCourseQueryHandler(ICourseQueries courseQueries) : IQueryHandler<GetCourseQuery, CourseDetails>
{
    /// <inheritdoc />
    public async Task<Result<CourseDetails>> Handle(GetCourseQuery query, CancellationToken cancellationToken)
    {
        var course = await courseQueries.GetAsync(query.CourseId, cancellationToken);

        return course is null ? Result.Failure<CourseDetails>(ApplicationErrors.NotFound<Course>()) : course;
    }
}
