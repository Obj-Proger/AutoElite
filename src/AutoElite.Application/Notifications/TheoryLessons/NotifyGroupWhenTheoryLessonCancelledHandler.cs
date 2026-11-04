using AutoElite.Application.Common.Events;
using AutoElite.Application.Common.Localization;

namespace AutoElite.Application.Notifications.TheoryLessons;

/// <summary>Tells the students of the group that a theory lesson was cancelled, with the reason (FR-5.2).</summary>
internal sealed class NotifyGroupWhenTheoryLessonCancelledHandler(GroupNotifier groupNotifier)
    : IDomainEventHandler<TheoryLessonCancelledDomainEvent>
{
    /// <inheritdoc />
    public Task Handle(TheoryLessonCancelledDomainEvent domainEvent, CancellationToken cancellationToken) =>
        groupNotifier.NotifyStudentsAsync(
            domainEvent.GroupId,
            domainEvent.StartUtc,
            NotificationType.TheoryLessonChanged,
            EntityReference.Create(EntityKind.TheoryLesson, domainEvent.LessonId).Value,
            NotificationTexts.TheoryLessonCancelled,
            domainEvent.OccurredOnUtc,
            (groupName, localStart) => [groupName, localStart, domainEvent.Reason],
            cancellationToken);
}
