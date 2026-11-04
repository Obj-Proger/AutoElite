using AutoElite.Application.Common.Events;
using AutoElite.Application.Common.Localization;

namespace AutoElite.Application.Notifications.Exams;

/// <summary>Tells the students of the group that an exam was cancelled, with the reason.</summary>
internal sealed class NotifyGroupWhenExamCancelledHandler(IRepository<Exam> examRepository, GroupNotifier groupNotifier)
    : IDomainEventHandler<ExamCancelledDomainEvent>
{
    /// <inheritdoc />
    public async Task Handle(ExamCancelledDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        var exam = await examRepository.GetByIdAsync(domainEvent.ExamId, cancellationToken);
        if (exam is null)
            return;

        await groupNotifier.NotifyStudentsAsync(
            domainEvent.GroupId,
            exam.ScheduledAtUtc,
            NotificationType.ExamChanged,
            EntityReference.Create(EntityKind.Exam, exam.Id).Value,
            NotificationTexts.ExamCancelled,
            domainEvent.OccurredOnUtc,
            (groupName, localTime) => [NotificationTexts.Term(exam.Kind), groupName, localTime, domainEvent.Reason],
            cancellationToken);
    }
}
