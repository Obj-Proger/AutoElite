using AutoElite.Application.Contracts;

namespace AutoElite.Application.Payments.RegisterPayment;

/// <summary>Records the payment on behalf of the caller and, if asked, confirms it.</summary>
internal sealed class RegisterPaymentCommandHandler(
    IRepository<Payment> paymentRepository,
    ContractAccess contractAccess,
    ICurrentUser currentUser,
    TimeProvider timeProvider) : ICommandHandler<RegisterPaymentCommand, Guid>
{
    /// <inheritdoc />
    public async Task<Result<Guid>> Handle(RegisterPaymentCommand command, CancellationToken cancellationToken)
    {
        var contractResult = await contractAccess.LoadAsync(command.ContractId, cancellationToken);
        if (contractResult.IsFailure)
            return Result.Failure<Guid>(contractResult.Error);

        var contract = contractResult.Value;
        var amountResult = Money.Create(command.Amount, contract.Price.Currency);
        if (amountResult.IsFailure)
            return Result.Failure<Guid>(amountResult.Error);

        var utcNow = timeProvider.GetUtcNow().UtcDateTime;
        var paymentResult = Payment.Register(
            contract,
            amountResult.Value,
            command.Method,
            command.Purpose,
            command.ExternalReference,
            currentUser.GetRequiredUserId(),
            utcNow);
        if (paymentResult.IsFailure)
            return Result.Failure<Guid>(paymentResult.Error);

        var payment = paymentResult.Value;
        if (command.ConfirmNow)
        {
            var confirmResult = payment.Confirm(contract, utcNow);
            if (confirmResult.IsFailure)
                return Result.Failure<Guid>(confirmResult.Error);
        }

        paymentRepository.Add(payment);

        return payment.Id;
    }
}
