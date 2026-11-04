namespace AutoElite.Application.TheoryLessons.MarkAttendance;

/// <summary>
/// Marks or corrects the attendance of students at a lesson that has started (FR-5.3), usually the whole group at
/// once. Each student marked present is counted on their contract; a correction to absent takes the count back. The
/// lesson is completed once every current member is marked. All marks are saved together or not at all.
/// </summary>
/// <param name="LessonId">The identifier of the lesson.</param>
/// <param name="Marks">The marks, at most one per contract.</param>
[RequireRoles(UserRole.TheoryInstructor)]
public sealed record MarkAttendanceCommand(Guid LessonId, IReadOnlyList<AttendanceMark>? Marks) : ICommand;
