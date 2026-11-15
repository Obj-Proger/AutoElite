namespace AutoElite.Application.StudentCards.GetStudentProgress;

/// <summary>
/// Reads the progress of a student for an instructor who teaches them (FR-4.7): a driving instructor whose slot they
/// booked, or the theory instructor of their group. The money of the contracts is left out.
/// </summary>
/// <param name="StudentId">The student.</param>
[RequireRoles(UserRole.DrivingInstructor, UserRole.TheoryInstructor)]
public sealed record GetStudentProgressQuery(Guid StudentId) : IQuery<StudentProgress>;
