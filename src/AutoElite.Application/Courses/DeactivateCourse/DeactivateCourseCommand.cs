namespace AutoElite.Application.Courses.DeactivateCourse;

/// <summary>Withdraws a course from the catalog: no new contracts or groups can use it, existing ones keep it (FR-3.3).</summary>
/// <param name="CourseId">The identifier of the course.</param>
[RequireRoles(UserRole.Administrator)]
public sealed record DeactivateCourseCommand(Guid CourseId) : ICommand;
