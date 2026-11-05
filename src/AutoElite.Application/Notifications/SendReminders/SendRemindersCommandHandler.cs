using AutoElite.Application.Common.Localization;
using AutoElite.Application.Contracts;
using AutoElite.Application.DrivingSlots;
using AutoElite.Application.Exams;
using AutoElite.Application.Settings;
using AutoElite.Application.StudyGroups;
using AutoElite.Application.TheoryLessons;

namespace AutoElite.Application.Notifications.SendReminders;

/// <summary>
/// Finds the lessons and exams whose reminder is due (<see cref="LessonReminderSchedule"/>) and notifies their
/// participants.
/// </summary>
/// <remarks>
/// A participant who already got this reminder, that is a reminder about the lesson or exam created at or after its
/// moment, is skipped. So is a student who booked the lesson or joined the group after the moment: they have just
/// learned about it.
/// </remarks>
internal sealed class SendRemindersCommandHandler(
    SchoolSettingsProvider settingsProvider,
    IDrivingSlotRepository slotRepository,
    ITheoryLessonRepository lessonRepository,
    IExamRepository examRepository,
    IContractRepository contractRepository,
    IStudyGroupRepository groupRepository,
    INotificationRepository notificationRepository,
    LocalTimes localTimes,
    Notifier notifier,
    TimeProvider timeProvider) : ICommandHandler<SendRemindersCommand>
{
    /// <inheritdoc />
    public async Task<Result> Handle(SendRemindersCommand command, CancellationToken cancellationToken)
    {
        var leadTimes = (await settingsProvider.GetAsync(cancellationToken)).LessonReminderLeadTimes;
        if (leadTimes.Count == 0)
            return Result.Success();

        var utcNow = timeProvider.GetUtcNow().UtcDateTime;
        var longest = leadTimes.Max();
        var slots = await slotRepository.GetBookedStartingBetweenAsync(utcNow, utcNow + longest, cancellationToken);
        var lessons = await lessonRepository.GetScheduledStartingBetweenAsync(utcNow, utcNow + longest, cancellationToken);
        var exams = await examRepository.GetScheduledBetweenAsync(utcNow, utcNow + longest, cancellationToken);

        if (slots.Count > 0 || lessons.Count > 0)
        {
            EntityReference[] subjects = [.. slots.Select(SlotSubject), .. lessons.Select(LessonSubject)];
            var sent = await notificationRepository.GetSentSinceAsync(
                NotificationType.LessonReminder, subjects, utcNow - longest, cancellationToken);

            foreach (var slot in slots)
                await RemindAsync(slot, leadTimes, sent, utcNow, cancellationToken);

            foreach (var lesson in lessons)
                await RemindAsync(lesson, leadTimes, sent, utcNow, cancellationToken);
        }

        if (exams.Count > 0)
        {
            var sent = await notificationRepository.GetSentSinceAsync(
                NotificationType.ExamReminder, [.. exams.Select(ExamSubject)], utcNow - longest, cancellationToken);

            foreach (var exam in exams)
                await RemindAsync(exam, leadTimes, sent, utcNow, cancellationToken);
        }

        return Result.Success();
    }

    private static EntityReference SlotSubject(DrivingSlot slot) => EntityReference.Create(EntityKind.DrivingSlot, slot.Id).Value;

    private static EntityReference LessonSubject(TheoryLesson lesson) => EntityReference.Create(EntityKind.TheoryLesson, lesson.Id).Value;

    private static EntityReference ExamSubject(Exam exam) => EntityReference.Create(EntityKind.Exam, exam.Id).Value;

    private static bool AlreadyReminded(
        IReadOnlyList<SentNotification> sent, Guid recipientId, EntityReference subject, DateTime momentUtc) =>
        sent.Any(notification => notification.RecipientId == recipientId && notification.Subject == subject
            && notification.CreatedAtUtc >= momentUtc);

    private async Task RemindAsync(
        DrivingSlot slot,
        IReadOnlyList<TimeSpan> leadTimes,
        IReadOnlyList<SentNotification> sent,
        DateTime utcNow,
        CancellationToken cancellationToken)
    {
        if (slot.CurrentBooking is not { } booking
            || LessonReminderSchedule.DueLeadTime(leadTimes, slot.Time.StartUtc, utcNow) is not { } leadTime)
            return;

        var momentUtc = slot.Time.StartUtc - leadTime;
        if (booking.BookedAtUtc > momentUtc)
            return;

        var subject = SlotSubject(slot);
        Guid[] participants = [booking.StudentId, slot.InstructorId];
        var recipientIds = participants.Where(userId => !AlreadyReminded(sent, userId, subject, momentUtc)).ToList();
        if (recipientIds.Count == 0)
            return;

        var contract = await contractRepository.GetByIdAsync(booking.ContractId, cancellationToken);
        var localStart = await localTimes.ToLocalAsync(slot.Time.StartUtc, contract?.BranchId, cancellationToken);

        await notifier.NotifyAllAsync(
            recipientIds, NotificationType.LessonReminder, subject, NotificationTexts.DrivingLessonReminder, utcNow,
            cancellationToken, localStart);
    }

    private async Task RemindAsync(
        TheoryLesson lesson,
        IReadOnlyList<TimeSpan> leadTimes,
        IReadOnlyList<SentNotification> sent,
        DateTime utcNow,
        CancellationToken cancellationToken)
    {
        if (LessonReminderSchedule.DueLeadTime(leadTimes, lesson.Time.StartUtc, utcNow) is not { } leadTime)
            return;

        var group = await groupRepository.GetByIdAsync(lesson.GroupId, cancellationToken);
        if (group is null)
            return;

        var momentUtc = lesson.Time.StartUtc - leadTime;
        var subject = LessonSubject(lesson);
        var participants = group.Enrollments
            .Where(enrollment => enrollment.IsCurrent && enrollment.EnrolledAtUtc <= momentUtc)
            .Select(enrollment => enrollment.StudentId)
            .Append(lesson.InstructorId);
        var recipientIds = participants.Where(userId => !AlreadyReminded(sent, userId, subject, momentUtc)).ToList();
        if (recipientIds.Count == 0)
            return;

        var localStart = await localTimes.ToLocalAsync(lesson.Time.StartUtc, group.BranchId, cancellationToken);

        await notifier.NotifyAllAsync(
            recipientIds, NotificationType.LessonReminder, subject, NotificationTexts.TheoryLessonReminder, utcNow,
            cancellationToken, group.Name, localStart);
    }

    private async Task RemindAsync(
        Exam exam,
        IReadOnlyList<TimeSpan> leadTimes,
        IReadOnlyList<SentNotification> sent,
        DateTime utcNow,
        CancellationToken cancellationToken)
    {
        if (LessonReminderSchedule.DueLeadTime(leadTimes, exam.ScheduledAtUtc, utcNow) is not { } leadTime)
            return;

        var group = await groupRepository.GetByIdAsync(exam.GroupId, cancellationToken);
        if (group is null)
            return;

        var momentUtc = exam.ScheduledAtUtc - leadTime;
        var subject = ExamSubject(exam);
        var participants = group.Enrollments
            .Where(enrollment => enrollment.IsCurrent && enrollment.EnrolledAtUtc <= momentUtc)
            .Select(enrollment => enrollment.StudentId)
            .Append(group.TheoryInstructorId);
        var recipientIds = participants.Where(userId => !AlreadyReminded(sent, userId, subject, momentUtc)).ToList();
        if (recipientIds.Count == 0)
            return;

        var localTime = await localTimes.ToLocalAsync(exam.ScheduledAtUtc, group.BranchId, cancellationToken);

        await notifier.NotifyAllAsync(
            recipientIds, NotificationType.ExamReminder, subject, NotificationTexts.ExamReminder, utcNow,
            cancellationToken, NotificationTexts.Term(exam.Kind), group.Name, localTime);
    }
}
