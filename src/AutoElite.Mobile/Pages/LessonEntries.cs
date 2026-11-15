using System.Globalization;
using AutoElite.Client.Api.Models;
using AutoElite.Client.Offline;
using AutoElite.Client.Offline.Actions;
using AutoElite.Mobile.Localization;
using AutoElite.Mobile.Schedule;
using Microsoft.Kiota.Abstractions;

namespace AutoElite.Mobile.Pages;

/// <summary>
/// Makes the cards of the lessons, with what the user can do with them, for the home page and the schedule: the lessons
/// of a student, whose driving booking can be cancelled before the lesson and rated after it, and the free slots they
/// can book; the practice slots of a driving instructor, who cancels a booking before the lesson and completes it or
/// records that the student did not come once it has started, and withdraws a slot that has not started; and the theory
/// lessons of a theory instructor, who moves or cancels a lesson before it starts and marks the attendance once it has
/// started. Every change but withdrawing, moving and cancelling, which need the network, goes through the offline
/// queue; a lesson with a change that waits to be sent shows it and offers nothing more until it is sent. After a
/// change, <see cref="Changed"/> reads the page again.
/// </summary>
/// <param name="synchronizer">Sends the actions, at once or once the API can be reached.</param>
/// <param name="offers">Withdraws the slots of a driving instructor.</param>
/// <param name="plans">Cancels the theory lessons of a theory instructor.</param>
/// <param name="timeProvider">The clock.</param>
public sealed class LessonEntries(
    OfflineSynchronizer synchronizer, SlotOffers offers, TheoryPlans plans, TimeProvider timeProvider)
{
    private const int MaxNoteLength = 2000;

    /// <summary>Gets or sets what reads the page again after a change.</summary>
    public Func<Task>? Changed { get; set; }

    /// <summary>Gives the slot or the theory lesson a waiting action is about, so that its lesson shows the change waiting.</summary>
    /// <param name="action">The action.</param>
    /// <returns>The slot or the lesson, or <see langword="null"/> for an action about neither.</returns>
    public static Guid? SubjectOf(OfflineAction action) =>
        action switch
        {
            BookDrivingSlot booking => booking.SlotId,
            CancelDrivingBooking cancellation => cancellation.SlotId,
            MarkDrivingNoShow noShow => noShow.SlotId,
            CompleteDrivingLesson completion => completion.SlotId,
            MarkTheoryAttendance attendance => attendance.LessonId,
            RateDrivingLesson rating => rating.SlotId,
            _ => null
        };

    /// <summary>Makes the card of a theory lesson of a student.</summary>
    /// <param name="item">The lesson with the attendance of the student.</param>
    /// <returns>The card, or <see langword="null"/> for a lesson without its times.</returns>
    public static ScheduleEntry? StudentTheory(StudentTheoryLessonItem item)
    {
        if (item is not { Lesson: { StartUtc: { } start, EndUtc: { } end } lesson })
            return null;

        return new ScheduleEntry
        {
            Kind = AppTexts.Get("Lesson.Theory"),
            Category = lesson.Status == TheoryLessonStatus.Cancelled ? ScheduleCategory.Inactive : ScheduleCategory.Theory,
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
            Start = start,
            End = end
        };
    }

    /// <summary>Makes the card of a driving lesson of a student.</summary>
    /// <param name="lesson">The lesson with its booking.</param>
    /// <param name="waiting">The slots and lessons with a change that waits to be sent.</param>
    /// <returns>The card, or <see langword="null"/> for a cancelled booking or a lesson without its times.</returns>
    public ScheduleEntry? StudentDriving(StudentDrivingLessonItem lesson, ISet<Guid> waiting)
    {
        ArgumentNullException.ThrowIfNull(waiting);
        if (lesson is not { Slot: { Id: { } slotId, StartUtc: { } start, EndUtc: { } end } slot, Booking.Status: { } status }
            || status == DrivingBookingStatus.Cancelled)
        {
            return null;
        }

        var now = timeProvider.GetUtcNow();
        var isWaiting = waiting.Contains(slotId);
        var actions = new List<EntryAction>();
        var rating = lesson.Booking?.StudentRating;
        if (status == DrivingBookingStatus.Active && start > now && !isWaiting)
            actions.Add(new EntryAction(AppTexts.Get("Lesson.Cancel"), new Command(async () => await CancelBookingAsync(slotId))));

        if (status == DrivingBookingStatus.Completed && rating is null && !isWaiting)
            actions.Add(new EntryAction(AppTexts.Get("Lesson.Rate"), new Command(async () => await RateAsync(slotId))));

        return new ScheduleEntry
        {
            Kind = AppTexts.Get("Lesson.Driving"),
            Category = status == DrivingBookingStatus.NoShow ? ScheduleCategory.Inactive : ScheduleCategory.Driving,
            Title = slot.VehicleName ?? string.Empty,
            Time = LessonTexts.TimeOf(start, end),
            Details = LessonTexts.DetailsOf(slot.InstructorName, slot.VehiclePlate, slot.TrainingGroundName),
            Status = status switch
            {
                _ when isWaiting => AppTexts.Get("Lesson.Waiting"),
                DrivingBookingStatus.Completed when rating is { } given => AppTexts.Format("Lesson.CompletedRated", given),
                DrivingBookingStatus.Completed => AppTexts.Get("Lesson.Completed"),
                DrivingBookingStatus.NoShow => AppTexts.Get("Lesson.NoShow"),
                _ => null
            },
            Actions = actions,
            Start = start,
            End = end
        };
    }

    /// <summary>
    /// Makes the card of a free slot a student can book, with the contracts that can book it; a student with more than
    /// one chooses the contract when they book.
    /// </summary>
    /// <param name="slot">The slot.</param>
    /// <param name="contracts">The active contracts that can book the slot.</param>
    /// <param name="waiting">The slots and lessons with a change that waits to be sent.</param>
    /// <returns>The card, or <see langword="null"/> for a slot without its times.</returns>
    public ScheduleEntry? FreeSlot(DrivingSlotItem slot, IReadOnlyList<ContractListItem> contracts, ISet<Guid> waiting)
    {
        ArgumentNullException.ThrowIfNull(contracts);
        ArgumentNullException.ThrowIfNull(waiting);
        if (slot is not { Id: { } slotId, StartUtc: { } start, EndUtc: { } end })
            return null;

        var isWaiting = waiting.Contains(slotId);
        var actions = new List<EntryAction>();
        if (!isWaiting && contracts.Count > 0)
            actions.Add(new EntryAction(AppTexts.Get("Lesson.Book"), new Command(async () => await BookAsync(slotId, contracts))));

        return new ScheduleEntry
        {
            Kind = AppTexts.Get("Lesson.FreeSlot"),
            Category = ScheduleCategory.Free,
            Title = slot.VehicleName ?? AppTexts.Get("Lesson.FreeSlot"),
            Time = LessonTexts.TimeOf(start, end),
            Details = LessonTexts.DetailsOf(slot.InstructorName, slot.VehiclePlate, slot.TrainingGroundName),
            Status = isWaiting ? AppTexts.Get("Lesson.Waiting") : null,
            Actions = actions,
            Start = start,
            End = end
        };
    }

    /// <summary>Makes the card of a practice slot of a driving instructor.</summary>
    /// <param name="item">The slot with its booking.</param>
    /// <param name="waiting">The slots and lessons with a change that waits to be sent.</param>
    /// <returns>The card, or <see langword="null"/> for a withdrawn slot or one without its times.</returns>
    public ScheduleEntry? DrivingSlot(DrivingScheduleItem item, ISet<Guid> waiting)
    {
        ArgumentNullException.ThrowIfNull(waiting);
        if (item is not { Slot: { Id: { } slotId, StartUtc: { } start, EndUtc: { } end, Status: { } slotStatus } slot }
            || slotStatus == DrivingSlotStatus.Withdrawn)
        {
            return null;
        }

        var now = timeProvider.GetUtcNow();
        var booking = item.Booking;
        var student = booking?.StudentName ?? string.Empty;
        var isWaiting = waiting.Contains(slotId);
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

        if (start > now && !isWaiting && slotStatus is DrivingSlotStatus.Available or DrivingSlotStatus.Booked)
        {
            var isBookedNow = booking?.Status == DrivingBookingStatus.Active;
            actions.Add(new EntryAction(
                AppTexts.Get("Lesson.Withdraw"), new Command(async () => await WithdrawAsync(slotId, isBookedNow))));
        }

        string? status = null;
        if (isWaiting)
            status = AppTexts.Get("Lesson.Waiting");
        else if (booking is { Status: DrivingBookingStatus.Completed, ActualHours: var hours })
            status = AppTexts.Format("Lesson.CompletedHours", hours?.ToString("0.##", CultureInfo.CurrentCulture));
        else if (booking is { Status: DrivingBookingStatus.NoShow })
            status = AppTexts.Get("Lesson.StudentNoShow");

        var isBooked = booking is { Status: not DrivingBookingStatus.Cancelled };

        return new ScheduleEntry
        {
            Kind = AppTexts.Get("Lesson.Driving"),
            Category = booking?.Status switch
            {
                DrivingBookingStatus.NoShow => ScheduleCategory.Inactive,
                _ when isBooked => ScheduleCategory.Driving,
                _ => ScheduleCategory.Free
            },
            Title = isBooked ? student : AppTexts.Get("Lesson.Free"),
            Time = LessonTexts.TimeOf(start, end),
            Details = LessonTexts.DetailsOf(slot.VehicleName, slot.VehiclePlate, slot.TrainingGroundName),
            Status = status,
            Actions = actions,
            Start = start,
            End = end
        };
    }

    /// <summary>Makes the card of a theory lesson of a theory instructor.</summary>
    /// <param name="lesson">The lesson.</param>
    /// <param name="waiting">The slots and lessons with a change that waits to be sent.</param>
    /// <returns>The card, or <see langword="null"/> for a lesson without its times.</returns>
    public ScheduleEntry? TheoryLesson(TheoryLessonItem lesson, ISet<Guid> waiting)
    {
        ArgumentNullException.ThrowIfNull(waiting);
        if (lesson is not { Id: { } lessonId, StartUtc: { } start, EndUtc: { } end, Status: { } status })
            return null;

        var now = timeProvider.GetUtcNow();
        var isWaiting = waiting.Contains(lessonId);
        var actions = new List<EntryAction>();
        if (status != TheoryLessonStatus.Cancelled && start <= now && !isWaiting)
        {
            var route = string.Create(
                CultureInfo.InvariantCulture, $"{Routes.Attendance}?{AttendanceViewModel.LessonParameter}={lessonId:D}");
            actions.Add(new EntryAction(AppTexts.Get("Lesson.MarkAttendance"), new Command(async () => await Routes.OpenAsync(route))));
        }

        if (status == TheoryLessonStatus.Scheduled && start > now && !isWaiting)
        {
            var title = LessonTexts.DetailsOf(lesson.TopicTitle, lesson.GroupName);
            var request = new TheoryLessonRequest(start.ToLocalTime(), end.ToLocalTime(), lessonId, title);
            actions.Add(new EntryAction(
                AppTexts.Get("Lesson.Move"),
                new Command(async () => await Routes.OpenAsync(
                    Routes.PlanTheory, new Dictionary<string, object> { [PlanTheoryViewModel.LessonParameter] = request }))));
            actions.Add(new EntryAction(
                AppTexts.Get("Lesson.CancelLesson"), new Command(async () => await CancelTheoryAsync(lessonId))));
        }

        return new ScheduleEntry
        {
            Kind = AppTexts.Get("Lesson.Theory"),
            Category = status == TheoryLessonStatus.Cancelled ? ScheduleCategory.Inactive : ScheduleCategory.Theory,
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
            Start = start,
            End = end
        };
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

    // A student with more than one contract that can book the slot chooses the one the lesson is taken under.
    private async Task BookAsync(Guid slotId, IReadOnlyList<ContractListItem> contracts)
    {
        var contract = contracts[0];
        if (contracts.Count > 1)
        {
            string[] names = [.. contracts.Select(option => LessonTexts.DetailsOf(option.CourseName, option.Number))];
            var choice = await Shell.Current.DisplayActionSheetAsync(
                AppTexts.Get("Lesson.ChooseContract"), AppTexts.Get("Common.Cancel"), null, names);
            var index = Array.IndexOf(names, choice);
            if (index < 0)
                return;

            contract = contracts[index];
        }

        if (contract.Id is { } contractId)
            await SubmitAsync(new BookDrivingSlot(slotId, contractId));
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

    // The instructor gives the reason, which a booked student reads; the slot is gone from the schedule once withdrawn.
    private async Task WithdrawAsync(Guid slotId, bool isBooked)
    {
        var reason = await Shell.Current.DisplayPromptAsync(
            AppTexts.Get("Lesson.WithdrawTitle"),
            isBooked ? AppTexts.Get("Lesson.WithdrawBookedReason") : AppTexts.Get("Lesson.WithdrawReason"),
            AppTexts.Get("Lesson.Withdraw"),
            AppTexts.Get("Lesson.Keep"),
            maxLength: MaxNoteLength);
        if (string.IsNullOrWhiteSpace(reason))
            return;

        try
        {
            await offers.WithdrawAsync(slotId, reason.Trim(), CancellationToken.None);
        }
        catch (Exception exception) when (exception is ApiException or HttpRequestException or TaskCanceledException)
        {
            await Shell.Current.DisplayAlertAsync(
                AppTexts.Get("Lesson.WithdrawTitle"), ProblemTexts.Of(exception), AppTexts.Get("Common.Ok"));

            return;
        }

        if (Changed is { } changed)
            await changed();
    }

    // The instructor gives the reason, which the students of the group read.
    private async Task CancelTheoryAsync(Guid lessonId)
    {
        var reason = await Shell.Current.DisplayPromptAsync(
            AppTexts.Get("Lesson.CancelLessonTitle"),
            AppTexts.Get("Lesson.CancelLessonReason"),
            AppTexts.Get("Lesson.CancelLesson"),
            AppTexts.Get("Lesson.Keep"),
            maxLength: MaxNoteLength);
        if (string.IsNullOrWhiteSpace(reason))
            return;

        try
        {
            await plans.CancelAsync(lessonId, reason.Trim(), CancellationToken.None);
        }
        catch (Exception exception) when (exception is ApiException or HttpRequestException or TaskCanceledException)
        {
            await Shell.Current.DisplayAlertAsync(
                AppTexts.Get("Lesson.CancelLessonTitle"), ProblemTexts.Of(exception), AppTexts.Get("Common.Ok"));

            return;
        }

        if (Changed is { } changed)
            await changed();
    }

    // The student chooses from 5 down to 1 and may add a comment; a low rating reaches the managers of the school.
    private async Task RateAsync(Guid slotId)
    {
        string[] choices =
        [
            AppTexts.Get("Rating.Five"),
            AppTexts.Get("Rating.Four"),
            AppTexts.Get("Rating.Three"),
            AppTexts.Get("Rating.Two"),
            AppTexts.Get("Rating.One")
        ];
        var choice = await Shell.Current.DisplayActionSheetAsync(
            AppTexts.Get("Lesson.RateTitle"), AppTexts.Get("Common.Cancel"), null, choices);
        var index = Array.IndexOf(choices, choice);
        if (index < 0)
            return;

        var comment = await Shell.Current.DisplayPromptAsync(
            AppTexts.Get("Lesson.RateTitle"),
            AppTexts.Get("Lesson.RateComment"),
            AppTexts.Get("Lesson.RateSend"),
            AppTexts.Get("Lesson.NoComment"),
            maxLength: MaxNoteLength);
        await SubmitAsync(new RateDrivingLesson(slotId, 5 - index, string.IsNullOrWhiteSpace(comment) ? null : comment.Trim()));
    }

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
        await synchronizer.SubmitAsync(action, CancellationToken.None);
        if (Changed is { } changed)
            await changed();
    }
}
