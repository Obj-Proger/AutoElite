namespace AutoElite.Domain.Events;

/// <summary>Raised when a payment is refunded; the student is notified.</summary>
/// <param name="PaymentId">The identifier of the payment.</param>
/// <param name="ContractId">The identifier of the contract.</param>
/// <param name="StudentId">The identifier of the student.</param>
/// <param name="Amount">The refunded amount in major units.</param>
/// <param name="Currency">The ISO 4217 currency code.</param>
/// <param name="OccurredOnUtc">The UTC time of the refund.</param>
public sealed record PaymentRefundedDomainEvent(
    Guid PaymentId, Guid ContractId, Guid StudentId, decimal Amount, string Currency, DateTime OccurredOnUtc) : IDomainEvent;
