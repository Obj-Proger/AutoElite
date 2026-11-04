using AutoElite.Application.Common.Events;
using AutoElite.Application.Common.Localization;

namespace AutoElite.Application.Notifications.Leads;

/// <summary>Tells a manager that a lead was assigned to them, unless they took it themselves.</summary>
internal sealed class NotifyWhenLeadAssignedHandler(IRepository<Lead> leadRepository, Notifier notifier)
    : IDomainEventHandler<LeadAssignedDomainEvent>
{
    /// <inheritdoc />
    public async Task Handle(LeadAssignedDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        if (domainEvent.AssignedByUserId == domainEvent.ManagerId)
            return;

        var lead = await leadRepository.GetByIdAsync(domainEvent.LeadId, cancellationToken);
        if (lead is null || lead.ManagerId != domainEvent.ManagerId)
            return;

        await notifier.NotifyAsync(
            domainEvent.ManagerId,
            NotificationType.LeadAssigned,
            EntityReference.Create(EntityKind.Lead, lead.Id).Value,
            NotificationTexts.LeadAssigned,
            domainEvent.OccurredOnUtc,
            cancellationToken,
            lead.ContactName);
    }
}
