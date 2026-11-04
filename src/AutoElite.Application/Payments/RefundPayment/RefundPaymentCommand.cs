namespace AutoElite.Application.Payments.RefundPayment;

/// <summary>
/// Returns a confirmed payment in full (FR-11.3). The amount is taken off the contract's paid amount in the same
/// transaction, so the debt grows back.
/// </summary>
/// <param name="PaymentId">The identifier of the payment.</param>
/// <param name="Reason">Why the money is returned. Required.</param>
[RequireRoles(UserRole.Administrator, UserRole.Manager)]
public sealed record RefundPaymentCommand(Guid PaymentId, string? Reason) : ICommand;
