namespace AutoElite.Application.TheoryLessons.ChangeTheoryLessonInstructor;

/// <summary>Hands a scheduled lesson to a substitute theory instructor, who must be free at that time.</summary>
/// <param name="LessonId">The identifier of the lesson.</param>
/// <param name="InstructorId">The instructor who will give the lesson.</param>
[RequireRoles(UserRole.TheoryInstructor)]
public sealed record ChangeTheoryLessonInstructorCommand(Guid LessonId, Guid InstructorId) : ICommand;
