using AutoElite.Application.Common.Events;
using AutoElite.Application.Common.Localization;

namespace AutoElite.Application.Notifications.Exams;

/// <summary>Tells the student the result of an exam, both when it is recorded and when it is corrected.</summary>
internal sealed class NotifyWhenExamResultRecordedHandler(Notifier notifier)
    : IDomainEventHandler<ExamResultRecordedDomainEvent>
{
    /// <inheritdoc />
    public Task Handle(ExamResultRecordedDomainEvent domainEvent, CancellationToken cancellationToken) =>
        notifier.NotifyAsync(
            domainEvent.StudentId,
            NotificationType.ExamResult,
            EntityReference.Create(EntityKind.Exam, domainEvent.ExamId).Value,
            domainEvent.Passed ? NotificationTexts.ExamPassed : NotificationTexts.ExamFailed,
            domainEvent.OccurredOnUtc,
            cancellationToken,
            NotificationTexts.Term(domainEvent.Kind));
}
