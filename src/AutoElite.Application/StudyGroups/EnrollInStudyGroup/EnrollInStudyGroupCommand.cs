namespace AutoElite.Application.StudyGroups.EnrollInStudyGroup;

/// <summary>
/// Enrolls the student of an active contract in a group of the same course (FR-4.6). The group must have a free
/// place; a contract and a group of different branches cannot be joined.
/// </summary>
/// <param name="GroupId">The identifier of the group.</param>
/// <param name="ContractId">The identifier of the contract.</param>
[RequireRoles(UserRole.Administrator, UserRole.Manager)]
public sealed record EnrollInStudyGroupCommand(Guid GroupId, Guid ContractId) : ICommand<Guid>;
