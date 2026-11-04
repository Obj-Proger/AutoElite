namespace AutoElite.Application.TheoryLessons.CancelTheoryLesson;

/// <summary>Cancels a scheduled lesson that has no attendance yet, with a reason; the students are notified (FR-5.2).</summary>
/// <param name="LessonId">The identifier of the lesson.</param>
/// <param name="Reason">Why the lesson is cancelled. Required.</param>
[RequireRoles(UserRole.TheoryInstructor)]
public sealed record CancelTheoryLessonCommand(Guid LessonId, string? Reason) : ICommand;
