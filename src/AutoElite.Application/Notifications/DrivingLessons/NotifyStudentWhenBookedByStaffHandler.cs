using AutoElite.Application.Common.Events;
using AutoElite.Application.Common.Localization;
using AutoElite.Application.Users;

namespace AutoElite.Application.Notifications.DrivingLessons;

/// <summary>
/// Tells a student that a manager or an administrator booked a driving lesson for them, with its time and instructor;
/// a student who booked the lesson themselves knows it already.
/// </summary>
internal sealed class NotifyStudentWhenBookedByStaffHandler(
    DrivingLessonReader lessonReader,
    IUserRepository userRepository,
    Notifier notifier) : IDomainEventHandler<DrivingLessonBookedDomainEvent>
{
    /// <inheritdoc />
    public async Task Handle(DrivingLessonBookedDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        if (domainEvent.BookedByUserId is null || domainEvent.BookedByUserId == domainEvent.StudentId)
            return;

        var lesson = await lessonReader.LoadAsync(domainEvent.SlotId, domainEvent.BookingId, cancellationToken);
        if (lesson is not { Booking.Status: DrivingBookingStatus.Active })
            return;

        var instructor = await userRepository.GetByIdAsync(domainEvent.InstructorId, cancellationToken);
        if (instructor is null)
            return;

        await notifier.NotifyAsync(
            domainEvent.StudentId,
            NotificationType.DrivingLessonBooked,
            lesson.Subject,
            NotificationTexts.DrivingLessonBookedForStudent,
            domainEvent.OccurredOnUtc,
            cancellationToken,
            lesson.LocalStart,
            instructor.FullName.ToString());
    }
}
