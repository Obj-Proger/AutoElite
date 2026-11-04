using AutoElite.Application.Common.Events;
using AutoElite.Application.Common.Localization;
using AutoElite.Application.Users;

namespace AutoElite.Application.Notifications.Leads;

/// <summary>
/// Tells the managers who can see a new lead that it arrived, as long as nobody is responsible for it yet. A lead
/// recorded by a manager is assigned to a manager at once, and that manager hears about it through the assignment.
/// </summary>
internal sealed class NotifyManagersWhenLeadCreatedHandler(
    IRepository<Lead> leadRepository,
    IUserRoleRepository roleRepository,
    Notifier notifier) : IDomainEventHandler<LeadCreatedDomainEvent>
{
    /// <inheritdoc />
    public async Task Handle(LeadCreatedDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        var lead = await leadRepository.GetByIdAsync(domainEvent.LeadId, cancellationToken);
        if (lead is not { ManagerId: null, IsOpen: true })
            return;

        var managers = await roleRepository.GetActiveHoldersAsync(UserRole.Manager, lead.BranchId, cancellationToken);

        notifier.NotifyAll(
            managers,
            NotificationType.NewLead,
            EntityReference.Create(EntityKind.Lead, lead.Id).Value,
            NotificationTexts.NewLead,
            domainEvent.OccurredOnUtc,
            lead.ContactName,
            NotificationTexts.Term(lead.Source));
    }
}
