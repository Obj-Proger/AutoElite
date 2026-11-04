namespace AutoElite.Application.Payments.RefundPayment;

/// <summary>Refunds the payment against its contract.</summary>
internal sealed class RefundPaymentCommandHandler(PaymentAccess paymentAccess, TimeProvider timeProvider)
    : ICommandHandler<RefundPaymentCommand>
{
    /// <inheritdoc />
    public async Task<Result> Handle(RefundPaymentCommand command, CancellationToken cancellationToken)
    {
        var loadResult = await paymentAccess.LoadAsync(command.PaymentId, cancellationToken);
        if (loadResult.IsFailure)
            return Result.Failure(loadResult.Error);

        var (payment, contract) = loadResult.Value;

        return payment.Refund(contract, command.Reason, timeProvider.GetUtcNow().UtcDateTime);
    }
}
