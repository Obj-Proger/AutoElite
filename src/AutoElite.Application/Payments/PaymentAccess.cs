using AutoElite.Application.Contracts;

namespace AutoElite.Application.Payments;

/// <summary>A payment loaded for a staff action, with its contract.</summary>
/// <param name="Payment">The payment.</param>
/// <param name="Contract">The contract of the payment.</param>
internal sealed record PaymentWithContract(Payment Payment, Contract Contract);

/// <summary>
/// Loads a payment with its contract for a staff action. A payment whose contract is outside the caller's branch scope
/// is reported as not found.
/// </summary>
internal sealed class PaymentAccess(IRepository<Payment> paymentRepository, ContractAccess contractAccess)
{
    /// <summary>Loads the payment and its contract if the contract is inside the caller's scope.</summary>
    /// <param name="paymentId">The identifier of the payment.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    public async Task<Result<PaymentWithContract>> LoadAsync(Guid paymentId, CancellationToken cancellationToken)
    {
        var payment = await paymentRepository.GetByIdAsync(paymentId, cancellationToken);
        if (payment is null)
            return Result.Failure<PaymentWithContract>(ApplicationErrors.NotFound<Payment>());

        var contractResult = await contractAccess.LoadAsync(payment.ContractId, cancellationToken);

        return contractResult.IsFailure
            ? Result.Failure<PaymentWithContract>(ApplicationErrors.NotFound<Payment>())
            : new PaymentWithContract(payment, contractResult.Value);
    }
}
