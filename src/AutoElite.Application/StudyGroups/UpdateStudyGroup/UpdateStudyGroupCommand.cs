namespace AutoElite.Application.StudyGroups.UpdateStudyGroup;

/// <summary>Changes the details of an active group; the capacity cannot drop below the current members.</summary>
/// <param name="GroupId">The identifier of the group.</param>
/// <param name="Name">The name.</param>
/// <param name="TheoryInstructorId">The responsible theory instructor.</param>
/// <param name="StartDate">The first day of training.</param>
/// <param name="EndDate">The planned last day of training, later than the start.</param>
/// <param name="Capacity">The maximum number of students, from 1 to 100.</param>
[RequireRoles(UserRole.Administrator, UserRole.Manager)]
public sealed record UpdateStudyGroupCommand(
    Guid GroupId,
    string? Name,
    Guid TheoryInstructorId,
    DateOnly StartDate,
    DateOnly EndDate,
    int Capacity) : ICommand;
