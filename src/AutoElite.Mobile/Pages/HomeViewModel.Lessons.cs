using System.Globalization;
using AutoElite.Client.Api.Models;
using AutoElite.Client.Offline;
using AutoElite.Client.Offline.Actions;
using AutoElite.Mobile.Localization;
using AutoElite.Mobile.Schedule;
using Microsoft.Kiota.Abstractions;

namespace AutoElite.Mobile.Pages;

/// <summary>
/// The lessons on the home page: those of a student, whose driving booking can be cancelled before the lesson; the
/// practice slots of a driving instructor, who cancels a booking before the lesson and completes it or records that the
/// student did not come once it has started; and the theory lessons of a theory instructor, who marks the attendance
/// once a lesson has started. Every change goes through the offline queue; a lesson with a change that waits to be sent
/// shows it and offers nothing more until it is sent.
/// </summary>
public sealed partial class HomeViewModel
{
    private const int MaxNoteLength = 2000;

    // The slot or the theory lesson a waiting action is about, so that its lesson shows the change waiting.
    private static Guid? SubjectOf(OfflineAction action) =>
        action switch
        {
            BookDrivingSlot booking => booking.SlotId,
            CancelDrivingBooking cancellation => cancellation.SlotId,
            MarkDrivingNoShow noShow => noShow.SlotId,
            CompleteDrivingLesson completion => completion.SlotId,
            MarkTheoryAttendance attendance => attendance.LessonId,
            _ => null
        };

    private static ScheduleEntry? TheoryEntry(StudentTheoryLessonItem item)
    {
        if (item is not { Lesson: { StartUtc: { } start, EndUtc: { } end } lesson })
            return null;

        return new ScheduleEntry
        {
            Kind = AppTexts.Get("Lesson.Theory"),
            Title = lesson.TopicTitle ?? string.Empty,
            Time = LessonTexts.TimeOf(start, end),
            Details = LessonTexts.DetailsOf(lesson.InstructorName, lesson.GroupName),
            Status = (lesson.Status, item.IsPresent) switch
            {
                (TheoryLessonStatus.Cancelled, _) => AppTexts.Get("Lesson.Cancelled"),
                (_, true) => AppTexts.Get("Lesson.Attended"),
                (_, false) => AppTexts.Get("Lesson.Absent"),
                (TheoryLessonStatus.Completed, _) => AppTexts.Get("Lesson.Completed"),
                _ => null
            },
            Start = start
        };
    }

    // Adds the lessons of the student and gives when they were read, if they come from the device.
    private async Task<DateTimeOffset?> LoadStudentLessonsAsync(List<ScheduleEntry> entries, HashSet<Guid> waitingSlots)
    {
        StudentLessons lessons;
        try
        {
            lessons = await _schedule.ReadAsync(CancellationToken.None);
        }
        catch (Exception exception) when (exception is ApiException or HttpRequestException)
        {
            // Never read on this device and no network: the page shows these lessons once it can read them.
            return null;
        }

        var now = _timeProvider.GetUtcNow();
        entries.AddRange(lessons.Driving.Value.Select(lesson => StudentDrivingEntry(lesson, waitingSlots, now)).OfType<ScheduleEntry>());
        entries.AddRange(lessons.Theory.Value.Select(TheoryEntry).OfType<ScheduleEntry>());

        return Earliest(
            lessons.Driving.IsFromCache ? lessons.Driving.SavedAt : null,
            lessons.Theory.IsFromCache ? lessons.Theory.SavedAt : null);
    }

    // Adds the slots of the driving instructor and gives when they were read, if they come from the device.
    private async Task<DateTimeOffset?> LoadInstructorLessonsAsync(List<ScheduleEntry> entries, HashSet<Guid> waitingSlots)
    {
        Cached<IReadOnlyList<DrivingScheduleItem>> slots;
        try
        {
            slots = await _instructorSchedule.ReadDrivingAsync(CancellationToken.None);
        }
        catch (Exception exception) when (exception is ApiException or HttpRequestException)
        {
            return null;
        }

        var now = _timeProvider.GetUtcNow();
        entries.AddRange(slots.Value.Select(slot => InstructorEntry(slot, waitingSlots, now)).OfType<ScheduleEntry>());

        return slots.IsFromCache ? slots.SavedAt : null;
    }

    // Adds the theory lessons of the theory instructor and gives when they were read, if they come from the device.
    private async Task<DateTimeOffset?> LoadTheoryInstructorLessonsAsync(List<ScheduleEntry> entries, HashSet<Guid> waiting)
    {
        Cached<IReadOnlyList<TheoryLessonItem>> lessons;
        try
        {
            lessons = await _instructorSchedule.ReadTheoryAsync(CancellationToken.None);
        }
        catch (Exception exception) when (exception is ApiException or HttpRequestException)
        {
            return null;
        }

        var now = _timeProvider.GetUtcNow();
        entries.AddRange(lessons.Value.Select(lesson => TheoryInstructorEntry(lesson, waiting, now)).OfType<ScheduleEntry>());

        return lessons.IsFromCache ? lessons.SavedAt : null;
    }

    private static ScheduleEntry? TheoryInstructorEntry(TheoryLessonItem lesson, HashSet<Guid> waiting, DateTimeOffset now)
    {
        if (lesson is not { Id: { } lessonId, StartUtc: { } start, EndUtc: { } end, Status: { } status })
            return null;

        var isWaiting = waiting.Contains(lessonId);
        var actions = new List<EntryAction>();
        if (status != TheoryLessonStatus.Cancelled && start <= now && !isWaiting)
        {
            var route = string.Create(
                CultureInfo.InvariantCulture, $"{Routes.Attendance}?{AttendanceViewModel.LessonParameter}={lessonId:D}");
            actions.Add(new EntryAction(AppTexts.Get("Lesson.MarkAttendance"), new Command(async () => await Routes.OpenAsync(route))));
        }

        return new ScheduleEntry
        {
            Kind = AppTexts.Get("Lesson.Theory"),
            Title = lesson.TopicTitle ?? string.Empty,
            Time = LessonTexts.TimeOf(start, end),
            Details = LessonTexts.DetailsOf(lesson.GroupName, lesson.InstructorName),
            Status = status switch
            {
                _ when isWaiting => AppTexts.Get("Lesson.Waiting"),
                TheoryLessonStatus.Cancelled => AppTexts.Get("Lesson.Cancelled"),
                TheoryLessonStatus.Completed => AppTexts.Get("Lesson.Completed"),
                _ => null
            },
            Actions = actions,
            Start = start
        };
    }

    private ScheduleEntry? StudentDrivingEntry(StudentDrivingLessonItem lesson, HashSet<Guid> waitingSlots, DateTimeOffset now)
    {
        if (lesson is not { Slot: { Id: { } slotId, StartUtc: { } start, EndUtc: { } end } slot, Booking.Status: { } status }
            || status == DrivingBookingStatus.Cancelled)
        {
            return null;
        }

        var isWaiting = waitingSlots.Contains(slotId);
        var actions = new List<EntryAction>();
        if (status == DrivingBookingStatus.Active && start > now && !isWaiting)
            actions.Add(new EntryAction(AppTexts.Get("Lesson.Cancel"), new Command(async () => await CancelBookingAsync(slotId))));

        return new ScheduleEntry
        {
            Kind = AppTexts.Get("Lesson.Driving"),
            Title = slot.VehicleName ?? string.Empty,
            Time = LessonTexts.TimeOf(start, end),
            Details = LessonTexts.DetailsOf(slot.InstructorName, slot.VehiclePlate, slot.TrainingGroundName),
            Status = status switch
            {
                _ when isWaiting => AppTexts.Get("Lesson.Waiting"),
                DrivingBookingStatus.Completed => AppTexts.Get("Lesson.Completed"),
                DrivingBookingStatus.NoShow => AppTexts.Get("Lesson.NoShow"),
                _ => null
            },
            Actions = actions,
            Start = start
        };
    }

    private ScheduleEntry? InstructorEntry(DrivingScheduleItem item, HashSet<Guid> waitingSlots, DateTimeOffset now)
    {
        if (item is not { Slot: { Id: { } slotId, StartUtc: { } start, EndUtc: { } end, Status: { } slotStatus } slot }
            || slotStatus == DrivingSlotStatus.Withdrawn)
        {
            return null;
        }

        var booking = item.Booking;
        var student = booking?.StudentName ?? string.Empty;
        var isWaiting = waitingSlots.Contains(slotId);
        var actions = new List<EntryAction>();
        if (booking?.Status == DrivingBookingStatus.Active && !isWaiting)
        {
            if (start <= now)
            {
                actions.Add(new EntryAction(
                    AppTexts.Get("Lesson.Complete"),
                    new Command(async () => await CompleteAsync(slotId, start, end, student))));
                actions.Add(new EntryAction(
                    AppTexts.Get("Lesson.MarkNoShow"), new Command(async () => await MarkNoShowAsync(slotId))));
            }
            else
            {
                actions.Add(new EntryAction(
                    AppTexts.Get("Lesson.Cancel"), new Command(async () => await CancelBookingAsync(slotId))));
            }
        }

        string? status = null;
        if (isWaiting)
            status = AppTexts.Get("Lesson.Waiting");
        else if (booking is { Status: DrivingBookingStatus.Completed, ActualHours: var hours })
            status = AppTexts.Format("Lesson.CompletedHours", hours?.ToString("0.##", CultureInfo.CurrentCulture));
        else if (booking is { Status: DrivingBookingStatus.NoShow })
            status = AppTexts.Get("Lesson.StudentNoShow");

        return new ScheduleEntry
        {
            Kind = AppTexts.Get("Lesson.Driving"),
            Title = booking?.StudentName ?? AppTexts.Get("Lesson.Free"),
            Time = LessonTexts.TimeOf(start, end),
            Details = LessonTexts.DetailsOf(slot.VehicleName, slot.VehiclePlate, slot.TrainingGroundName),
            Status = status,
            Actions = actions,
            Start = start
        };
    }

    private async Task CancelBookingAsync(Guid slotId)
    {
        var reason = await Shell.Current.DisplayPromptAsync(
            AppTexts.Get("Lesson.CancelTitle"),
            AppTexts.Get("Lesson.CancelReason"),
            AppTexts.Get("Lesson.CancelConfirm"),
            AppTexts.Get("Lesson.Keep"),
            maxLength: MaxNoteLength);
        if (!string.IsNullOrWhiteSpace(reason))
            await SubmitAsync(new CancelDrivingBooking(slotId, reason.Trim()));
    }

    // The completion page asks for the hours, the grades and the note, and sends the completion through the queue.
    private static Task CompleteAsync(Guid slotId, DateTimeOffset start, DateTimeOffset end, string student) =>
        Routes.OpenAsync(
            Routes.Completion,
            new Dictionary<string, object>
            {
                [CompletionViewModel.RequestParameter] = new CompletionRequest(
                    slotId, Math.Round((decimal)(end - start).TotalHours, 2), student, LessonTexts.TimeOf(start, end))
            });

    private async Task MarkNoShowAsync(Guid slotId)
    {
        if (await Shell.Current.DisplayAlertAsync(
            AppTexts.Get("Lesson.NoShowTitle"),
            AppTexts.Get("Lesson.NoShowMessage"),
            AppTexts.Get("Lesson.MarkNoShow"),
            AppTexts.Get("Common.Cancel")))
        {
            await SubmitAsync(new MarkDrivingNoShow(slotId));
        }
    }

    // Sent at once when the API can be reached, later otherwise; the lesson shows the change waiting until then.
    private async Task SubmitAsync(OfflineAction action)
    {
        await _synchronizer.SubmitAsync(action, CancellationToken.None);
        await LoadAsync();
    }
}
