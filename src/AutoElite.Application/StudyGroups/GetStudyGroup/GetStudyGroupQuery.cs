namespace AutoElite.Application.StudyGroups.GetStudyGroup;

/// <summary>
/// Reads the card of a group with its members. Staff see the groups of their scope; a theory instructor sees only
/// their own groups.
/// </summary>
/// <param name="GroupId">The identifier of the group.</param>
[RequireRoles(UserRole.Administrator, UserRole.Manager, UserRole.TheoryInstructor)]
public sealed record GetStudyGroupQuery(Guid GroupId) : IQuery<StudyGroupDetails>;
