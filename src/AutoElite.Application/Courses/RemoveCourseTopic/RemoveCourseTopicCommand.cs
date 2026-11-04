namespace AutoElite.Application.Courses.RemoveCourseTopic;

/// <summary>
/// Removes a topic from the curriculum. A topic that questions, theory lessons or test templates refer to cannot be
/// removed, so that their history stays intact.
/// </summary>
/// <param name="CourseId">The identifier of the course.</param>
/// <param name="TopicId">The identifier of the topic.</param>
[RequireRoles(UserRole.Administrator)]
public sealed record RemoveCourseTopicCommand(Guid CourseId, Guid TopicId) : ICommand;
