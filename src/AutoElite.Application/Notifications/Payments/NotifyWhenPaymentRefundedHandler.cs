using AutoElite.Application.Common.Events;
using AutoElite.Application.Common.Localization;
using AutoElite.Application.Contracts;

namespace AutoElite.Application.Notifications.Payments;

/// <summary>Tells the student that a payment under their contract was refunded.</summary>
internal sealed class NotifyWhenPaymentRefundedHandler(Notifier notifier, IContractRepository contractRepository)
    : IDomainEventHandler<PaymentRefundedDomainEvent>
{
    /// <inheritdoc />
    public async Task Handle(PaymentRefundedDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        var contract = await contractRepository.GetByIdAsync(domainEvent.ContractId, cancellationToken);
        if (contract is null)
            return;

        await notifier.NotifyAsync(
            domainEvent.StudentId,
            NotificationType.PaymentRefunded,
            EntityReference.Create(EntityKind.Payment, domainEvent.PaymentId).Value,
            NotificationTexts.PaymentRefunded,
            domainEvent.OccurredOnUtc,
            cancellationToken,
            domainEvent.Amount,
            domainEvent.Currency,
            contract.Number);
    }
}
