using AutoElite.Application.Common.Events;
using AutoElite.Application.Common.Localization;

namespace AutoElite.Application.Notifications.StaffTasks;

/// <summary>
/// Tells a staff member that they were given a task, unless they set it themselves; this includes the next
/// occurrence of a repeating task that someone else completed.
/// </summary>
internal sealed class NotifyWhenStaffTaskAssignedHandler(
    IRepository<StaffTask> taskRepository,
    StaffTaskNotices taskNotices,
    Notifier notifier) : IDomainEventHandler<StaffTaskAssignedDomainEvent>
{
    /// <inheritdoc />
    public async Task Handle(StaffTaskAssignedDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        if (domainEvent.AssignedByUserId == domainEvent.AssigneeId)
            return;

        var task = await taskRepository.GetByIdAsync(domainEvent.TaskId, cancellationToken);
        if (task is null || task.AssigneeId != domainEvent.AssigneeId || task.Status == StaffTaskStatus.Done)
            return;

        await notifier.NotifyAsync(
            task.AssigneeId,
            NotificationType.TaskAssigned,
            EntityReference.Create(EntityKind.StaffTask, task.Id).Value,
            NotificationTexts.TaskAssigned,
            domainEvent.OccurredOnUtc,
            cancellationToken,
            task.Title,
            await taskNotices.LocalDeadlineAsync(task, cancellationToken));
    }
}
