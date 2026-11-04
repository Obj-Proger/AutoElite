namespace AutoElite.Application.Payments.ConfirmPayment;

/// <summary>
/// Confirms that the money of a registered payment has arrived (FR-11.2). The amount is added to the contract's paid
/// amount in the same transaction, and the student is notified.
/// </summary>
/// <param name="PaymentId">The identifier of the payment.</param>
[RequireRoles(UserRole.Administrator, UserRole.Manager)]
public sealed record ConfirmPaymentCommand(Guid PaymentId) : ICommand;
