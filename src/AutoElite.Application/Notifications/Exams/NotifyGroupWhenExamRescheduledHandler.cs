using AutoElite.Application.Common.Events;
using AutoElite.Application.Common.Localization;

namespace AutoElite.Application.Notifications.Exams;

/// <summary>Tells the students of the group that an exam was moved to a new time.</summary>
internal sealed class NotifyGroupWhenExamRescheduledHandler(IRepository<Exam> examRepository, GroupNotifier groupNotifier)
    : IDomainEventHandler<ExamRescheduledDomainEvent>
{
    /// <inheritdoc />
    public async Task Handle(ExamRescheduledDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        var exam = await examRepository.GetByIdAsync(domainEvent.ExamId, cancellationToken);
        if (exam is null)
            return;

        await groupNotifier.NotifyStudentsAsync(
            domainEvent.GroupId,
            domainEvent.ScheduledAtUtc,
            NotificationType.ExamChanged,
            EntityReference.Create(EntityKind.Exam, exam.Id).Value,
            NotificationTexts.ExamRescheduled,
            domainEvent.OccurredOnUtc,
            (groupName, localTime) => [NotificationTexts.Term(exam.Kind), groupName, localTime],
            cancellationToken);
    }
}
