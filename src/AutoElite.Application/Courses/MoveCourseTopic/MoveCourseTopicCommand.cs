namespace AutoElite.Application.Courses.MoveCourseTopic;

/// <summary>Moves a topic to another place in the curriculum; the other topics shift to close the gap.</summary>
/// <param name="CourseId">The identifier of the course.</param>
/// <param name="TopicId">The identifier of the topic.</param>
/// <param name="NewPosition">The one-based target position, from 1 to the number of topics.</param>
[RequireRoles(UserRole.Administrator)]
public sealed record MoveCourseTopicCommand(Guid CourseId, Guid TopicId, int NewPosition) : ICommand;
