namespace AutoElite.Application.Contracts.ChangeContractPrice;

/// <summary>
/// Changes the agreed price of an active contract, for example after a discount. The currency stays the contract's;
/// a price below the paid amount leaves an overpayment to refund.
/// </summary>
/// <param name="ContractId">The identifier of the contract.</param>
/// <param name="PriceAmount">The new price in the contract currency, greater than zero.</param>
[RequireRoles(UserRole.Administrator, UserRole.Manager)]
public sealed record ChangeContractPriceCommand(Guid ContractId, decimal PriceAmount) : ICommand;
