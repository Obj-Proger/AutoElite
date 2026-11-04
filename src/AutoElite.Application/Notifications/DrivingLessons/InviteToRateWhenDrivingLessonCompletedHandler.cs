using AutoElite.Application.Common.Events;
using AutoElite.Application.Common.Localization;

namespace AutoElite.Application.Notifications.DrivingLessons;

/// <summary>Invites the student to rate a driving lesson once the instructor completes it (FR-6.8).</summary>
internal sealed class InviteToRateWhenDrivingLessonCompletedHandler(DrivingLessonReader lessonReader, Notifier notifier)
    : IDomainEventHandler<DrivingLessonCompletedDomainEvent>
{
    /// <inheritdoc />
    public async Task Handle(DrivingLessonCompletedDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        var lesson = await lessonReader.LoadAsync(domainEvent.SlotId, domainEvent.BookingId, cancellationToken);
        if (lesson is not { Booking: { Status: DrivingBookingStatus.Completed, StudentRating: null } })
            return;

        await notifier.NotifyAsync(
            domainEvent.StudentId,
            NotificationType.RateDrivingLesson,
            lesson.Subject,
            NotificationTexts.RateDrivingLesson,
            domainEvent.OccurredOnUtc,
            cancellationToken,
            lesson.LocalStart);
    }
}
