namespace AutoElite.Application.Exams.GetGroupExams;

/// <summary>Reads all exams of a study group, the earliest first.</summary>
/// <param name="GroupId">The identifier of the group.</param>
[RequireRoles(UserRole.Administrator, UserRole.Manager, UserRole.TheoryInstructor, UserRole.DrivingInstructor)]
public sealed record GetGroupExamsQuery(Guid GroupId) : IQuery<IReadOnlyList<ExamItem>>;
