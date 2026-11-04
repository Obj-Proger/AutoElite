namespace AutoElite.Application.Contracts.CompleteContract;

/// <summary>Completes a contract after the training (FR-4.4). A contract with a debt cannot be completed.</summary>
/// <param name="ContractId">The identifier of the contract.</param>
/// <param name="CompletedOn">The day of completion: not before the start and not in the future.</param>
[RequireRoles(UserRole.Administrator, UserRole.Manager)]
public sealed record CompleteContractCommand(Guid ContractId, DateOnly CompletedOn) : ICommand;
