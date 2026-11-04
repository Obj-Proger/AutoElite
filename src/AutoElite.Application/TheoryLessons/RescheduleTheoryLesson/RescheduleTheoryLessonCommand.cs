namespace AutoElite.Application.TheoryLessons.RescheduleTheoryLesson;

/// <summary>
/// Moves a scheduled lesson that has no attendance yet; the students of the group are notified (FR-5.2).
/// </summary>
/// <param name="LessonId">The identifier of the lesson.</param>
/// <param name="StartUtc">The new start, in UTC and in the future.</param>
/// <param name="EndUtc">The new end, in UTC.</param>
[RequireRoles(UserRole.TheoryInstructor)]
public sealed record RescheduleTheoryLessonCommand(Guid LessonId, DateTime StartUtc, DateTime EndUtc) : ICommand;
