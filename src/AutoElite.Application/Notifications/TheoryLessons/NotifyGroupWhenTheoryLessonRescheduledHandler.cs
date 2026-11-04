using AutoElite.Application.Common.Events;
using AutoElite.Application.Common.Localization;

namespace AutoElite.Application.Notifications.TheoryLessons;

/// <summary>Tells the students of the group that a theory lesson was moved to a new time.</summary>
internal sealed class NotifyGroupWhenTheoryLessonRescheduledHandler(GroupNotifier groupNotifier)
    : IDomainEventHandler<TheoryLessonRescheduledDomainEvent>
{
    /// <inheritdoc />
    public Task Handle(TheoryLessonRescheduledDomainEvent domainEvent, CancellationToken cancellationToken) =>
        groupNotifier.NotifyStudentsAsync(
            domainEvent.GroupId,
            domainEvent.StartUtc,
            NotificationType.TheoryLessonChanged,
            EntityReference.Create(EntityKind.TheoryLesson, domainEvent.LessonId).Value,
            NotificationTexts.TheoryLessonRescheduled,
            domainEvent.OccurredOnUtc,
            (groupName, localStart) => [groupName, localStart],
            cancellationToken);
}
