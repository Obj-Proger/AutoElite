namespace AutoElite.Application.StudyGroups.DeactivateStudyGroup;

/// <summary>Closes a group when its training has finished or it is cancelled; current members can still be withdrawn.</summary>
/// <param name="GroupId">The identifier of the group.</param>
[RequireRoles(UserRole.Administrator, UserRole.Manager)]
public sealed record DeactivateStudyGroupCommand(Guid GroupId) : ICommand;
