namespace AutoElite.Application.Payments.CancelPayment;

/// <summary>Cancels the payment; the domain checks the status and the reason.</summary>
internal sealed class CancelPaymentCommandHandler(PaymentAccess paymentAccess, TimeProvider timeProvider)
    : ICommandHandler<CancelPaymentCommand>
{
    /// <inheritdoc />
    public async Task<Result> Handle(CancelPaymentCommand command, CancellationToken cancellationToken)
    {
        var loadResult = await paymentAccess.LoadAsync(command.PaymentId, cancellationToken);

        return loadResult.IsFailure
            ? Result.Failure(loadResult.Error)
            : loadResult.Value.Payment.Cancel(command.Reason, timeProvider.GetUtcNow().UtcDateTime);
    }
}
