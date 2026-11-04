namespace AutoElite.Application.StudyGroups.WithdrawFromStudyGroup;

/// <summary>
/// Withdraws the student of a contract from a group, for example to move them to another group. The enrollment stays
/// in the history of the group.
/// </summary>
/// <param name="GroupId">The identifier of the group.</param>
/// <param name="ContractId">The identifier of the contract.</param>
[RequireRoles(UserRole.Administrator, UserRole.Manager)]
public sealed record WithdrawFromStudyGroupCommand(Guid GroupId, Guid ContractId) : ICommand;
