namespace AutoElite.Application.Courses.AddCourseTopic;

/// <summary>Adds a topic to the end of a course curriculum (FR-3.2).</summary>
/// <param name="CourseId">The identifier of the course.</param>
/// <param name="Title">The title, unique within the course, ignoring case.</param>
/// <param name="Description">An optional description.</param>
[RequireRoles(UserRole.Administrator)]
public sealed record AddCourseTopicCommand(Guid CourseId, string? Title, string? Description) : ICommand<Guid>;
