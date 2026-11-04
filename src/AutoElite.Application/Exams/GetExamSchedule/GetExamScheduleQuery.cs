namespace AutoElite.Application.Exams.GetExamSchedule;

/// <summary>
/// Reads the exams scheduled within a period, the earliest first. Staff and driving instructors see the exams of the
/// groups of their branch and of groups without one; a theory instructor sees the exams of their own groups.
/// </summary>
/// <param name="FromUtc">The start of the period in UTC.</param>
/// <param name="ToUtc">The end of the period in UTC; the period lasts at most 92 days.</param>
/// <param name="Kind">Only theory or only practice exams.</param>
/// <param name="GroupId">Only exams of this group.</param>
[RequireRoles(UserRole.Administrator, UserRole.Manager, UserRole.TheoryInstructor, UserRole.DrivingInstructor)]
public sealed record GetExamScheduleQuery(DateTime FromUtc, DateTime ToUtc, ExamKind? Kind = null, Guid? GroupId = null)
    : IQuery<IReadOnlyList<ExamItem>>;
