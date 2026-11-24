using System.Globalization;
using AutoElite.Client.Api.Models;
using AutoElite.Client.Offline;
using AutoElite.Mobile.Localization;
using AutoElite.Mobile.Schedule;
using Microsoft.Kiota.Abstractions;

namespace AutoElite.Mobile.Pages;

/// <summary>
/// The lessons on the page of today, those of a student, of a driving instructor or of a theory instructor, read for
/// the next weeks; <see cref="LessonEntries"/> makes them with what the user can do with them, and
/// <see cref="TodayCards"/> the highlighted card of the next one and the rows of the following ones. A student also
/// sees the progress and the debt of their active contract.
/// </summary>
public sealed partial class HomeViewModel
{
    // The lessons after the next one that the page shows; the schedule has the rest.
    private const int RowCount = 5;

    // The free slots of an instructor are counted for this many days ahead.
    private static readonly TimeSpan _week = TimeSpan.FromDays(7);

    // The next lesson in its card, the following ones as rows, and the figures of the day of an instructor.
    private void ShowLessons()
    {
        var now = _timeProvider.GetUtcNow();
        var next = TodayCards.NextOf(Lessons, now);
        Next = next is null ? null : TodayCards.CardOf(next, now);
        Rows =
        [
            .. Lessons.Where(lesson => lesson != next && lesson.End > now)
                .Take(RowCount)
                .Select(lesson => TodayCards.RowOf(lesson, now))
        ];

        var culture = CultureInfo.CurrentCulture;
        var today = now.ToLocalTime().Date;
        var ofDay = Lessons
            .Where(lesson => lesson.Start.ToLocalTime().Date == today
                && lesson.Category is ScheduleCategory.Driving or ScheduleCategory.Theory or ScheduleCategory.Inactive)
            .ToList();
        var held = ofDay.Count(lesson => lesson.End <= now);
        LessonsHeld = held.ToString(culture);
        LessonsOfDay = AppTexts.Format("Today.OfHeld", ofDay.Count);
        DayProgress = ofDay.Count > 0 ? (double)held / ofDay.Count : 0;
        FreeSlots = Lessons
            .Count(lesson => lesson.Category == ScheduleCategory.Free && lesson.Start > now && lesson.Start - now < _week)
            .ToString(culture);
    }

    // The hours driven, the theory attended and the debt of the active contract of a student; without a network the
    // last figures stay.
    private async Task ShowContractAsync()
    {
        try
        {
            var contracts = (await _contracts.ReadContractsAsync(CancellationToken.None)).Value;
            if (contracts.FirstOrDefault(contract => contract.Status == ContractStatus.Active) is not { Id: { } contractId })
            {
                HasContract = false;
                HasDebt = false;

                return;
            }

            var contract = (await _contracts.ReadDetailsAsync(contractId, CancellationToken.None)).Value;
            var culture = CultureInfo.CurrentCulture;
            var driven = contract.CompletedPracticeHours ?? 0;
            var course = contract.CoursePracticeHours ?? 0;
            DrivingHours = driven.ToString("0.#", culture);
            CourseHours = AppTexts.Format("Today.OfHours", course);
            DrivingProgress = course > 0 ? Math.Clamp(driven / course, 0, 1) : 0;
            TheoryAttended = (contract.AttendedTheoryLessons ?? 0).ToString(culture);
            HasContract = true;

            var debt = contract.DebtAmount ?? 0;
            Debt = AppTexts.Format("Today.Debt", string.Create(culture, $"{debt:N0} {contract.Currency}"));
            DebtContract = AppTexts.Format("Today.Contract", contract.Number);
            HasDebt = debt > 0;
        }
        catch (Exception exception) when (exception is ApiException or HttpRequestException)
        {
            // The figures are shown once the contract can be read.
        }
    }

    // Adds the lessons of the student and gives when they were read, if they come from the device.
    private async Task<DateTimeOffset?> LoadStudentLessonsAsync(List<ScheduleEntry> entries, HashSet<Guid> waitingSlots)
    {
        StudentLessons lessons;
        try
        {
            lessons = await _schedule.ReadAsync(CancellationToken.None);
            await _entries.ReadRulesAsync();
        }
        catch (Exception exception) when (exception is ApiException or HttpRequestException)
        {
            // Never read on this device and no network: the page shows these lessons once it can read them.
            return null;
        }

        entries.AddRange(lessons.Driving.Value.Select(lesson => _entries.StudentDriving(lesson, waitingSlots)).OfType<ScheduleEntry>());
        entries.AddRange(lessons.Theory.Value.Select(LessonEntries.StudentTheory).OfType<ScheduleEntry>());

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

        entries.AddRange(slots.Value.Select(slot => _entries.DrivingSlot(slot, waitingSlots)).OfType<ScheduleEntry>());

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

        entries.AddRange(lessons.Value.Select(lesson => _entries.TheoryLesson(lesson, waiting)).OfType<ScheduleEntry>());

        return lessons.IsFromCache ? lessons.SavedAt : null;
    }
}
