namespace AutoElite.Application.Courses.UpdateCourseTopic;

/// <summary>Changes the title and description of a topic.</summary>
/// <param name="CourseId">The identifier of the course.</param>
/// <param name="TopicId">The identifier of the topic.</param>
/// <param name="Title">The title, unique within the course, ignoring case.</param>
/// <param name="Description">An optional description; empty removes it.</param>
[RequireRoles(UserRole.Administrator)]
public sealed record UpdateCourseTopicCommand(Guid CourseId, Guid TopicId, string? Title, string? Description) : ICommand;
