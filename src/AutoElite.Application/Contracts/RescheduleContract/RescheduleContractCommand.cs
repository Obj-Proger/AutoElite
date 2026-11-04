namespace AutoElite.Application.Contracts.RescheduleContract;

/// <summary>Changes the training period of an active contract.</summary>
/// <param name="ContractId">The identifier of the contract.</param>
/// <param name="StartDate">The first day of training.</param>
/// <param name="EndDate">The planned last day of training, later than the start.</param>
[RequireRoles(UserRole.Administrator, UserRole.Manager)]
public sealed record RescheduleContractCommand(Guid ContractId, DateOnly StartDate, DateOnly EndDate) : ICommand;
