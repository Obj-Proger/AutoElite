using AutoElite.Application.Common.Events;
using AutoElite.Application.Common.Localization;

namespace AutoElite.Application.Notifications.Exams;

/// <summary>Tells the students of the group that an exam was scheduled.</summary>
internal sealed class NotifyGroupWhenExamScheduledHandler(GroupNotifier groupNotifier)
    : IDomainEventHandler<ExamScheduledDomainEvent>
{
    /// <inheritdoc />
    public Task Handle(ExamScheduledDomainEvent domainEvent, CancellationToken cancellationToken) =>
        groupNotifier.NotifyStudentsAsync(
            domainEvent.GroupId,
            domainEvent.ScheduledAtUtc,
            NotificationType.ExamChanged,
            EntityReference.Create(EntityKind.Exam, domainEvent.ExamId).Value,
            NotificationTexts.ExamScheduled,
            domainEvent.OccurredOnUtc,
            (groupName, localTime) => [NotificationTexts.Term(domainEvent.Kind), groupName, localTime],
            cancellationToken);
}
