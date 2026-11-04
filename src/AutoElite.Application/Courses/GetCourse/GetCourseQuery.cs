namespace AutoElite.Application.Courses.GetCourse;

/// <summary>Reads a course with its curriculum. Every signed-in user may read it.</summary>
/// <param name="CourseId">The identifier of the course.</param>
public sealed record GetCourseQuery(Guid CourseId) : IQuery<CourseDetails>;
