namespace AutoElite.Application.Payments.ConfirmPayment;

/// <summary>Confirms the payment against its contract.</summary>
internal sealed class ConfirmPaymentCommandHandler(PaymentAccess paymentAccess, TimeProvider timeProvider)
    : ICommandHandler<ConfirmPaymentCommand>
{
    /// <inheritdoc />
    public async Task<Result> Handle(ConfirmPaymentCommand command, CancellationToken cancellationToken)
    {
        var loadResult = await paymentAccess.LoadAsync(command.PaymentId, cancellationToken);
        if (loadResult.IsFailure)
            return Result.Failure(loadResult.Error);

        var (payment, contract) = loadResult.Value;

        return payment.Confirm(contract, timeProvider.GetUtcNow().UtcDateTime);
    }
}
