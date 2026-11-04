namespace AutoElite.Application.StudyGroups.CreateStudyGroup;

/// <summary>
/// Forms a study group for a course with a responsible theory instructor (FR-4.5). A manager's group goes to the
/// manager's branch unless another branch in the scope is given.
/// </summary>
/// <param name="Name">The name.</param>
/// <param name="CourseId">The course; it must be active.</param>
/// <param name="TheoryInstructorId">The responsible theory instructor.</param>
/// <param name="StartDate">The first day of training.</param>
/// <param name="EndDate">The planned last day of training, later than the start.</param>
/// <param name="Capacity">The maximum number of students, from 1 to 100.</param>
/// <param name="BranchId">The branch; empty for the manager's branch.</param>
[RequireRoles(UserRole.Administrator, UserRole.Manager)]
public sealed record CreateStudyGroupCommand(
    string? Name,
    Guid CourseId,
    Guid TheoryInstructorId,
    DateOnly StartDate,
    DateOnly EndDate,
    int Capacity,
    Guid? BranchId = null) : ICommand<Guid>;
