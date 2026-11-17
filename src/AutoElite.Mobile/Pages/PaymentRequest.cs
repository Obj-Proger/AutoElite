namespace AutoElite.Mobile.Pages;

/// <summary>The contract a manager records a payment towards, passed to the page of the payment.</summary>
/// <param name="ContractId">The contract.</param>
/// <param name="Title">The number and the course of the contract, shown at the top of the page.</param>
/// <param name="Currency">The ISO 4217 code of the currency of the contract.</param>
/// <param name="Debt">The amount still owed, which the page offers as the amount.</param>
public sealed record PaymentRequest(Guid ContractId, string Title, string? Currency, double Debt);
