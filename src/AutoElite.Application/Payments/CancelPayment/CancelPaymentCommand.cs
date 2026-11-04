namespace AutoElite.Application.Payments.CancelPayment;

/// <summary>
/// Cancels a registered payment that was recorded by mistake or never arrived. Payments are never deleted, so the
/// cancellation and its reason stay in the history.
/// </summary>
/// <param name="PaymentId">The identifier of the payment.</param>
/// <param name="Reason">Why the payment is cancelled. Required.</param>
[RequireRoles(UserRole.Administrator, UserRole.Manager)]
public sealed record CancelPaymentCommand(Guid PaymentId, string? Reason) : ICommand;
