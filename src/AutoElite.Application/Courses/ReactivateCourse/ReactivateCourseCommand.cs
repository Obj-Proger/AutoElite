namespace AutoElite.Application.Courses.ReactivateCourse;

/// <summary>Returns a withdrawn course to the catalog.</summary>
/// <param name="CourseId">The identifier of the course.</param>
[RequireRoles(UserRole.Administrator)]
public sealed record ReactivateCourseCommand(Guid CourseId) : ICommand;
