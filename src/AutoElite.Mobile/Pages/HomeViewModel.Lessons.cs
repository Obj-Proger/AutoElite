using AutoElite.Client.Api.Models;
using AutoElite.Client.Offline;
using AutoElite.Mobile.Schedule;
using Microsoft.Kiota.Abstractions;

namespace AutoElite.Mobile.Pages;

/// <summary>
/// The lessons on the home page, those of a student, of a driving instructor or of a theory instructor, read for the
/// next weeks; <see cref="LessonEntries"/> makes their cards with what the user can do with them.
/// </summary>
public sealed partial class HomeViewModel
{
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
