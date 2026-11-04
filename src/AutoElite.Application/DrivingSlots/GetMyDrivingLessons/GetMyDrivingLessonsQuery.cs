namespace AutoElite.Application.DrivingSlots.GetMyDrivingLessons;

/// <summary>
/// Reads the driving lessons of the signed-in student in a period, the earliest first: every booking, cancelled ones
/// included, with the hours, grades and note of completed lessons.
/// </summary>
/// <param name="FromUtc">The start of the period in UTC.</param>
/// <param name="ToUtc">The end of the period in UTC; the period lasts at most 92 days.</param>
[RequireRoles(UserRole.Student)]
public sealed record GetMyDrivingLessonsQuery(DateTime FromUtc, DateTime ToUtc) : IQuery<IReadOnlyList<StudentDrivingLessonItem>>;
