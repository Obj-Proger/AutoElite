using AutoElite.Application.Common.Events;
using AutoElite.Application.Common.Localization;
using AutoElite.Application.Users;

namespace AutoElite.Application.Notifications.DrivingLessons;

/// <summary>Tells the instructor that a student booked their driving lesson; only students book (FR-6.4).</summary>
internal sealed class NotifyInstructorWhenDrivingLessonBookedHandler(
    DrivingLessonReader lessonReader,
    IUserRepository userRepository,
    Notifier notifier) : IDomainEventHandler<DrivingLessonBookedDomainEvent>
{
    /// <inheritdoc />
    public async Task Handle(DrivingLessonBookedDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        var lesson = await lessonReader.LoadAsync(domainEvent.SlotId, domainEvent.BookingId, cancellationToken);
        if (lesson is not { Booking.Status: DrivingBookingStatus.Active })
            return;

        var student = await userRepository.GetByIdAsync(domainEvent.StudentId, cancellationToken);
        if (student is null)
            return;

        await notifier.NotifyAsync(
            domainEvent.InstructorId,
            NotificationType.DrivingLessonBooked,
            lesson.Subject,
            NotificationTexts.DrivingLessonBooked,
            domainEvent.OccurredOnUtc,
            cancellationToken,
            student.FullName.ToString(),
            lesson.LocalStart);
    }
}
