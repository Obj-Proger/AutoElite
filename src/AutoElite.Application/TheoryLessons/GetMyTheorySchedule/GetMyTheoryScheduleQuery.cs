namespace AutoElite.Application.TheoryLessons.GetMyTheorySchedule;

/// <summary>
/// Reads the theory lessons of the signed-in student that overlap a period, across all of their groups, with their
/// own attendance.
/// </summary>
/// <param name="FromUtc">The start of the period in UTC.</param>
/// <param name="ToUtc">The end of the period in UTC; the period lasts at most 92 days.</param>
[RequireRoles(UserRole.Student)]
public sealed record GetMyTheoryScheduleQuery(DateTime FromUtc, DateTime ToUtc) : IQuery<IReadOnlyList<StudentTheoryLessonItem>>;
