namespace AutoElite.Application.StudentCards.GetStudentCard;

/// <summary>
/// Reads the summary card of a student (FR-4.7): contracts, attendance, tests, grades and debt. Administrators see
/// every student; managers see the contracts of their branch; a student sees their own card.
/// </summary>
/// <param name="StudentId">The student.</param>
[RequireRoles(UserRole.Administrator, UserRole.Manager, UserRole.Student)]
public sealed record GetStudentCardQuery(Guid StudentId) : IQuery<StudentCard>;
