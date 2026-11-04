namespace AutoElite.Application.TheoryLessons.ScheduleTheoryLesson;

/// <summary>
/// Schedules a theory lesson of a group on a topic of its course (FR-5.1). The responsible instructor of the group
/// schedules it and gives it, unless another theory instructor is named as a substitute.
/// </summary>
/// <param name="GroupId">The study group.</param>
/// <param name="TopicId">A topic of the group's course.</param>
/// <param name="StartUtc">The start, in UTC and in the future.</param>
/// <param name="EndUtc">The end, in UTC; the lesson lasts at most 8 hours.</param>
/// <param name="InstructorId">The instructor giving the lesson; empty for the caller.</param>
[RequireRoles(UserRole.TheoryInstructor)]
public sealed record ScheduleTheoryLessonCommand(
    Guid GroupId,
    Guid TopicId,
    DateTime StartUtc,
    DateTime EndUtc,
    Guid? InstructorId = null) : ICommand<Guid>;
