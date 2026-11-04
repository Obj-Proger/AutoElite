namespace AutoElite.Application.Payments.SetPaymentReference;

/// <summary>Records the bank or gateway identifier of a registered payment, for example from a bank statement.</summary>
/// <param name="PaymentId">The identifier of the payment.</param>
/// <param name="ExternalReference">The identifier in the bank or gateway. Required.</param>
[RequireRoles(UserRole.Administrator, UserRole.Manager)]
public sealed record SetPaymentReferenceCommand(Guid PaymentId, string? ExternalReference) : ICommand;
