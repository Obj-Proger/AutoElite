using AutoElite.Application.Common.Events;
using AutoElite.Application.Common.Localization;
using AutoElite.Application.Users;

namespace AutoElite.Application.Notifications.DrivingLessons;

/// <summary>
/// Tells the managers responsible for the student's contract that a driving lesson got a low rating (FR-6.8): the
/// managers of the contract's branch and those not tied to a branch.
/// </summary>
internal sealed class NotifyManagersWhenDrivingLessonRatedLowHandler(
    DrivingLessonReader lessonReader,
    IUserRepository userRepository,
    IUserRoleRepository roleRepository,
    Notifier notifier) : IDomainEventHandler<LowDrivingLessonRatingDomainEvent>
{
    /// <inheritdoc />
    public async Task Handle(LowDrivingLessonRatingDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        var lesson = await lessonReader.LoadAsync(domainEvent.SlotId, domainEvent.BookingId, cancellationToken);
        if (lesson is null)
            return;

        var participants = await userRepository.GetByIdsAsync([domainEvent.StudentId, domainEvent.InstructorId], cancellationToken);
        var student = participants.SingleOrDefault(user => user.Id == domainEvent.StudentId);
        var instructor = participants.SingleOrDefault(user => user.Id == domainEvent.InstructorId);
        if (student is null || instructor is null)
            return;

        var managers = await roleRepository.GetActiveHoldersAsync(UserRole.Manager, lesson.BranchId, cancellationToken);

        notifier.NotifyAll(
            managers,
            NotificationType.LowDrivingLessonRating,
            lesson.Subject,
            NotificationTexts.LowDrivingLessonRating,
            domainEvent.OccurredOnUtc,
            student.FullName.ToString(),
            instructor.FullName.ToString(),
            lesson.LocalStart,
            domainEvent.Rating);
    }
}
