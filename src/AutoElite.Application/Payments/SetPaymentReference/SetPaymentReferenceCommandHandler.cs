namespace AutoElite.Application.Payments.SetPaymentReference;

/// <summary>Sets the reference; the domain checks the status and the value.</summary>
internal sealed class SetPaymentReferenceCommandHandler(PaymentAccess paymentAccess) : ICommandHandler<SetPaymentReferenceCommand>
{
    /// <inheritdoc />
    public async Task<Result> Handle(SetPaymentReferenceCommand command, CancellationToken cancellationToken)
    {
        var loadResult = await paymentAccess.LoadAsync(command.PaymentId, cancellationToken);

        return loadResult.IsFailure
            ? Result.Failure(loadResult.Error)
            : loadResult.Value.Payment.SetExternalReference(command.ExternalReference);
    }
}
