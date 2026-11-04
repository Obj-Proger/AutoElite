namespace AutoElite.Application.StudyGroups.ReactivateStudyGroup;

/// <summary>Reopens a closed group.</summary>
/// <param name="GroupId">The identifier of the group.</param>
[RequireRoles(UserRole.Administrator, UserRole.Manager)]
public sealed record ReactivateStudyGroupCommand(Guid GroupId) : ICommand;
