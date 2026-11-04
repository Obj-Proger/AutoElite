namespace AutoElite.Application.TheoryLessons.GetTheorySchedule;

/// <summary>
/// Reads the theory lessons that overlap a period, the earliest first (FR-5.1). Managers see the lessons of the groups
/// of their branch and of groups without a branch; a theory instructor sees the lessons they give and the lessons of
/// the groups they are responsible for.
/// </summary>
/// <param name="FromUtc">The start of the period in UTC.</param>
/// <param name="ToUtc">The end of the period in UTC; the period lasts at most 92 days.</param>
/// <param name="InstructorId">Only lessons this instructor gives or whose group this instructor is responsible for.</param>
/// <param name="GroupId">Only lessons of this group.</param>
/// <param name="BranchId">Only lessons of groups of this branch.</param>
/// <param name="Status">Only lessons in this status.</param>
[RequireRoles(UserRole.Administrator, UserRole.Manager, UserRole.TheoryInstructor)]
public sealed record GetTheoryScheduleQuery(
    DateTime FromUtc,
    DateTime ToUtc,
    Guid? InstructorId = null,
    Guid? GroupId = null,
    Guid? BranchId = null,
    TheoryLessonStatus? Status = null) : IQuery<IReadOnlyList<TheoryLessonItem>>;
