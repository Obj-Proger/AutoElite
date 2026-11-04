using AutoElite.Application.Common.Events;
using AutoElite.Application.Common.Localization;

namespace AutoElite.Application.Notifications.DrivingLessons;

/// <summary>
/// Tells the student and the instructor that a booking was cancelled, except the one who cancelled it. A booking
/// cancelled by a manager or by the system, for example when the contract closes, is reported to both.
/// </summary>
internal sealed class NotifyWhenDrivingBookingCancelledHandler(DrivingLessonReader lessonReader, Notifier notifier)
    : IDomainEventHandler<DrivingBookingCancelledDomainEvent>
{
    /// <inheritdoc />
    public async Task Handle(DrivingBookingCancelledDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        var lesson = await lessonReader.LoadAsync(domainEvent.SlotId, domainEvent.BookingId, cancellationToken);
        if (lesson is null)
            return;

        Guid[] participants = [domainEvent.StudentId, domainEvent.InstructorId];

        await notifier.NotifyAllAsync(
            participants.Where(participantId => participantId != domainEvent.CancelledByUserId),
            NotificationType.DrivingBookingCancelled,
            lesson.Subject,
            NotificationTexts.DrivingBookingCancelled,
            domainEvent.OccurredOnUtc,
            cancellationToken,
            lesson.LocalStart,
            domainEvent.Reason);
    }
}
