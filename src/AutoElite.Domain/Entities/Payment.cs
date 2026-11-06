using AutoElite.Domain.Enums;
using AutoElite.Domain.Events;
using AutoElite.Domain.ValueObjects;

namespace AutoElite.Domain.Entities;

/// <summary>
/// A payment towards a <see cref="Contract"/>, recorded by a manager or made online by the student.
/// </summary>
/// <remarks>
/// <para>
/// Confirming a payment adds it to the contract's paid amount and refunding takes it off, in the same call, so the
/// two can never disagree. The caller saves both aggregates in one unit of work.
/// </para>
/// <para>
/// Payments are never deleted: a payment recorded by mistake, or an online payment that failed, is cancelled with a
/// reason. A refund returns the whole payment.
/// </para>
/// </remarks>
public sealed class Payment : AuditableEntity
{
    /// <summary>The longest purpose.</summary>
    public const int MaxPurposeLength = 500;

    /// <summary>The longest external reference.</summary>
    public const int MaxExternalReferenceLength = 200;

    /// <summary>The longest reason of a refund or a cancellation.</summary>
    public const int MaxReasonLength = 1000;

    /// <summary>Gets the identifier of the contract the payment is for.</summary>
    public Guid ContractId { get; private set; }

    /// <summary>Gets the identifier of the student, copied from the contract for the student's payment history.</summary>
    public Guid StudentId { get; private set; }

    /// <summary>Gets the amount, in the contract currency.</summary>
    public Money Amount { get; private set; } = null!;

    /// <summary>Gets how the payment was made.</summary>
    public PaymentMethod Method { get; private set; }

    /// <summary>Gets what the payment is for, for example "Tuition, first instalment".</summary>
    public string Purpose { get; private set; } = null!;

    /// <summary>Gets the identifier of the payment in the gateway or bank, if any.</summary>
    public string? ExternalReference { get; private set; }

    /// <summary>
    /// Gets the identifier of the staff member who recorded the payment, or <see langword="null"/> for an online
    /// payment started by the student.
    /// </summary>
    public Guid? RegisteredByUserId { get; private set; }

    /// <summary>Gets the payment status.</summary>
    public PaymentStatus Status { get; private set; }

    /// <summary>Gets the UTC time the payment was recorded.</summary>
    public DateTime RegisteredAtUtc { get; private set; }

    /// <summary>Gets the UTC time the payment was confirmed, if it was.</summary>
    public DateTime? ConfirmedAtUtc { get; private set; }

    /// <summary>Gets the UTC time the payment was refunded, if it was.</summary>
    public DateTime? RefundedAtUtc { get; private set; }

    /// <summary>Gets the UTC time the payment was cancelled, if it was.</summary>
    public DateTime? CancelledAtUtc { get; private set; }

    /// <summary>Gets the reason for the refund or the cancellation, if any.</summary>
    public string? Reason { get; private set; }

    private Payment(
        Guid id, Contract contract, Money amount, PaymentMethod method, string purpose, string? externalReference,
        Guid? registeredByUserId, DateTime utcNow)
        : base(id)
    {
        ContractId = contract.Id;
        StudentId = contract.StudentId;
        Amount = amount;
        Method = method;
        Purpose = purpose;
        ExternalReference = externalReference;
        RegisteredByUserId = registeredByUserId;
        RegisteredAtUtc = utcNow;
        Status = PaymentStatus.Registered;
    }

    /// <summary>Required by EF Core for entity materialization.</summary>
    private Payment()
    {
    }

    /// <summary>Records a new payment with status <see cref="PaymentStatus.Registered"/>.</summary>
    /// <param name="contract">The contract the payment is for; payments are accepted for a contract in any status.</param>
    /// <param name="amount">The amount; must be positive and in the contract currency.</param>
    /// <param name="method">How the payment was made.</param>
    /// <param name="purpose">What the payment is for. Required.</param>
    /// <param name="externalReference">The identifier in the gateway or bank, if any.</param>
    /// <param name="registeredByUserId">The staff member recording the payment, or <see langword="null"/> for an online payment.</param>
    /// <param name="utcNow">The current UTC time.</param>
    /// <returns>The payment, or the first validation failure encountered.</returns>
    public static Result<Payment> Register(
        Contract contract,
        Money amount,
        PaymentMethod method,
        string? purpose,
        string? externalReference,
        Guid? registeredByUserId,
        DateTime utcNow)
    {
        if (!amount.HasSameCurrencyAs(contract.Price))
            return Result.Failure<Payment>(DomainErrors.Payment.CurrencyMismatch);

        if (!amount.IsPositive)
            return Result.Failure<Payment>(DomainErrors.Payment.AmountNotPositive);

        var methodResult = Guard.DefinedEnum(method, DomainErrors.Payment.MethodNotDefined);
        if (methodResult.IsFailure)
            return Result.Failure<Payment>(methodResult.Error);

        var purposeResult = Guard.RequiredText(purpose, MaxPurposeLength, DomainErrors.Payment.PurposeEmpty, DomainErrors.Payment.PurposeTooLong);
        if (purposeResult.IsFailure)
            return Result.Failure<Payment>(purposeResult.Error);

        var referenceResult = Guard.OptionalText(externalReference, MaxExternalReferenceLength, DomainErrors.Payment.ExternalReferenceTooLong);
        if (referenceResult.IsFailure)
            return Result.Failure<Payment>(referenceResult.Error);

        if (registeredByUserId is { } userId && userId == Guid.Empty)
            return Result.Failure<Payment>(DomainErrors.Payment.UserIdEmpty);

        return new Payment(
            Guid.CreateVersion7(), contract, amount, method, purposeResult.Value, referenceResult.Value, registeredByUserId, utcNow);
    }

    /// <summary>
    /// Confirms that the money was received, adds it to the contract's paid amount and raises
    /// <see cref="PaymentConfirmedDomainEvent"/>.
    /// </summary>
    /// <param name="contract">The contract of this payment.</param>
    /// <param name="utcNow">The current UTC time.</param>
    /// <returns>A failure if the payment is not registered, the contract is a different one, or the contract rejects the amount.</returns>
    public Result Confirm(Contract contract, DateTime utcNow)
    {
        if (Status != PaymentStatus.Registered)
            return Result.Failure(DomainErrors.Payment.NotRegistered);

        if (contract.Id != ContractId)
            return Result.Failure(DomainErrors.Payment.ContractMismatch);

        var applyResult = contract.ApplyPayment(Amount);
        if (applyResult.IsFailure)
            return applyResult;

        Status = PaymentStatus.Confirmed;
        ConfirmedAtUtc = utcNow;
        RaiseDomainEvent(new PaymentConfirmedDomainEvent(Id, ContractId, StudentId, Amount.Amount, Amount.Currency.Code, utcNow));

        return Result.Success();
    }

    /// <summary>
    /// Returns the whole payment, takes it off the contract's paid amount and raises <see cref="PaymentRefundedDomainEvent"/>.
    /// </summary>
    /// <param name="contract">The contract of this payment.</param>
    /// <param name="reason">Why the money is returned. Required.</param>
    /// <param name="utcNow">The current UTC time.</param>
    /// <returns>
    /// A failure if the payment is not confirmed, the contract is a different one, the reason is invalid,
    /// or the contract rejects the refund.
    /// </returns>
    public Result Refund(Contract contract, string? reason, DateTime utcNow)
    {
        if (Status != PaymentStatus.Confirmed)
            return Result.Failure(DomainErrors.Payment.NotConfirmed);

        if (contract.Id != ContractId)
            return Result.Failure(DomainErrors.Payment.ContractMismatch);

        var reasonResult = Guard.RequiredText(reason, MaxReasonLength, DomainErrors.Payment.ReasonEmpty, DomainErrors.Payment.ReasonTooLong);
        if (reasonResult.IsFailure)
            return Result.Failure(reasonResult.Error);

        var applyResult = contract.ApplyRefund(Amount);
        if (applyResult.IsFailure)
            return applyResult;

        Status = PaymentStatus.Refunded;
        RefundedAtUtc = utcNow;
        Reason = reasonResult.Value;
        RaiseDomainEvent(new PaymentRefundedDomainEvent(Id, ContractId, StudentId, Amount.Amount, Amount.Currency.Code, utcNow));

        return Result.Success();
    }

    /// <summary>Cancels a payment that was not confirmed: recorded by mistake, or an online payment that failed.</summary>
    /// <param name="reason">Why the payment is cancelled. Required.</param>
    /// <param name="utcNow">The current UTC time.</param>
    /// <returns>A failure if the payment is not registered or the reason is invalid.</returns>
    public Result Cancel(string? reason, DateTime utcNow)
    {
        if (Status != PaymentStatus.Registered)
            return Result.Failure(DomainErrors.Payment.NotRegistered);

        var reasonResult = Guard.RequiredText(reason, MaxReasonLength, DomainErrors.Payment.ReasonEmpty, DomainErrors.Payment.ReasonTooLong);
        if (reasonResult.IsFailure)
            return Result.Failure(reasonResult.Error);

        Status = PaymentStatus.Cancelled;
        CancelledAtUtc = utcNow;
        Reason = reasonResult.Value;

        return Result.Success();
    }

    /// <summary>Records the identifier the payment gateway assigned, for an online payment that has started.</summary>
    /// <returns>A failure if the payment is not registered, or the reference is empty or too long.</returns>
    public Result SetExternalReference(string? externalReference)
    {
        if (Status != PaymentStatus.Registered)
            return Result.Failure(DomainErrors.Payment.NotRegistered);

        var referenceResult = Guard.RequiredText(
            externalReference, MaxExternalReferenceLength, DomainErrors.Payment.ExternalReferenceEmpty, DomainErrors.Payment.ExternalReferenceTooLong);
        if (referenceResult.IsFailure)
            return Result.Failure(referenceResult.Error);

        ExternalReference = referenceResult.Value;

        return Result.Success();
    }
}
