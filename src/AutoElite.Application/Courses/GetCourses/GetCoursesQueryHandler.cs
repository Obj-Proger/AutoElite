namespace AutoElite.Application.Courses.GetCourses;

/// <summary>Reads the catalog.</summary>
internal sealed class GetCoursesQueryHandler(ICourseQueries courseQueries)
    : IQueryHandler<GetCoursesQuery, IReadOnlyList<CourseListItem>>
{
    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<CourseListItem>>> Handle(GetCoursesQuery query, CancellationToken cancellationToken) =>
        Result.Success(await courseQueries.GetListAsync(query.Category, query.IsActive, cancellationToken));
}
