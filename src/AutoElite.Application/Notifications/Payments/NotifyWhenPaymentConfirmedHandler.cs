using AutoElite.Application.Common.Events;
using AutoElite.Application.Common.Localization;
using AutoElite.Application.Contracts;

namespace AutoElite.Application.Notifications.Payments;

/// <summary>Tells the student that a payment under their contract was confirmed.</summary>
internal sealed class NotifyWhenPaymentConfirmedHandler(Notifier notifier, IContractRepository contractRepository)
    : IDomainEventHandler<PaymentConfirmedDomainEvent>
{
    /// <inheritdoc />
    public async Task Handle(PaymentConfirmedDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        var contract = await contractRepository.GetByIdAsync(domainEvent.ContractId, cancellationToken);
        if (contract is null)
            return;

        await notifier.NotifyAsync(
            domainEvent.StudentId,
            NotificationType.PaymentConfirmed,
            EntityReference.Create(EntityKind.Payment, domainEvent.PaymentId).Value,
            NotificationTexts.PaymentConfirmed,
            domainEvent.OccurredOnUtc,
            cancellationToken,
            domainEvent.Amount,
            domainEvent.Currency,
            contract.Number);
    }
}
