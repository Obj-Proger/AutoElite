using AutoElite.Application.Common.Events;
using AutoElite.Application.Common.Localization;

namespace AutoElite.Application.Notifications.StaffTasks;

/// <summary>
/// Tells the assignee that their task is past its deadline (FR-12.4). Nothing is sent if the task was completed,
/// reassigned or given a new deadline meanwhile.
/// </summary>
internal sealed class NotifyWhenStaffTaskOverdueHandler(
    IRepository<StaffTask> taskRepository,
    StaffTaskNotices taskNotices,
    Notifier notifier) : IDomainEventHandler<StaffTaskOverdueDomainEvent>
{
    /// <inheritdoc />
    public async Task Handle(StaffTaskOverdueDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        var task = await taskRepository.GetByIdAsync(domainEvent.TaskId, cancellationToken);
        if (task is null || task.Status == StaffTaskStatus.Done || task.AssigneeId != domainEvent.AssigneeId
            || task.DueAtUtc != domainEvent.DueAtUtc)
            return;

        await notifier.NotifyAsync(
            task.AssigneeId,
            NotificationType.TaskOverdue,
            EntityReference.Create(EntityKind.StaffTask, task.Id).Value,
            NotificationTexts.TaskOverdue,
            domainEvent.OccurredOnUtc,
            cancellationToken,
            task.Title,
            await taskNotices.LocalDeadlineAsync(task, cancellationToken));
    }
}
