namespace AutoElite.Application.TheoryLessons.GetGroupTheoryLessons;

/// <summary>
/// Reads all lessons of a study group, the earliest first, to follow the progress through the course. Staff see the
/// groups of their scope; a theory instructor sees the groups they are responsible for.
/// </summary>
/// <param name="GroupId">The identifier of the group.</param>
[RequireRoles(UserRole.Administrator, UserRole.Manager, UserRole.TheoryInstructor)]
public sealed record GetGroupTheoryLessonsQuery(Guid GroupId) : IQuery<IReadOnlyList<TheoryLessonItem>>;
