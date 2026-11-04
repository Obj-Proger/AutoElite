namespace AutoElite.Application.Contracts.TerminateContract;

/// <summary>
/// Terminates a contract early with a reason (FR-4.4). The student's future driving lessons are cancelled after the
/// commit, by the handler of the contract closed event.
/// </summary>
/// <param name="ContractId">The identifier of the contract.</param>
/// <param name="Reason">Why the contract is terminated. Required.</param>
/// <param name="TerminatedOn">The day of termination: not before the start and not in the future.</param>
[RequireRoles(UserRole.Administrator, UserRole.Manager)]
public sealed record TerminateContractCommand(Guid ContractId, string? Reason, DateOnly TerminatedOn) : ICommand;
