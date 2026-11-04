namespace AutoElite.Application.TheoryLessons.GetTheoryLesson;

/// <summary>
/// Reads the card of a theory lesson with the roster and the attendance marks (FR-5.3). Staff see the lessons of their scope;
/// a theory instructor sees the lessons they give and the lessons of the groups they are responsible for.
/// </summary>
/// <param name="LessonId">The identifier of the lesson.</param>
[RequireRoles(UserRole.Administrator, UserRole.Manager, UserRole.TheoryInstructor)]
public sealed record GetTheoryLessonQuery(Guid LessonId) : IQuery<TheoryLessonDetails>;
