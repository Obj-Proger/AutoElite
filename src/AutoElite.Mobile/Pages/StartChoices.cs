using System.Globalization;

namespace AutoElite.Mobile.Pages;

/// <summary>
/// The days and the times a lesson or a slot can start at, for the pickers of a page: the days from today, and every
/// half hour from 6:00 to 22:00, in the time zone of the device, with a given start among them.
/// </summary>
internal sealed class StartChoices
{
    // The days ahead to choose from at least, and the hours of the day a start can be at.
    private const int DaysAhead = 60;
    private const int FirstHour = 6;
    private const int LastHour = 22;

    private readonly IReadOnlyList<DateOnly> _days;
    private readonly IReadOnlyList<TimeOnly> _times;

    private StartChoices(IReadOnlyList<DateOnly> days, IReadOnlyList<TimeOnly> times, int dayIndex, int timeIndex)
    {
        var culture = CultureInfo.CurrentCulture;
        _days = days;
        _times = times;
        DayNames = [.. days.Select(day => day.ToString("ddd, d MMMM", culture))];
        TimeNames = [.. times.Select(time => time.ToString("t", culture))];
        DayIndex = dayIndex;
        TimeIndex = timeIndex;
    }

    /// <summary>Gets the days, as the user reads them.</summary>
    public IReadOnlyList<string> DayNames { get; }

    /// <summary>Gets the times, as the user reads them.</summary>
    public IReadOnlyList<string> TimeNames { get; }

    /// <summary>Gets the position of the day of the given start.</summary>
    public int DayIndex { get; }

    /// <summary>Gets the position of the time of the given start, at the half hour before it, within the hours.</summary>
    public int TimeIndex { get; }

    /// <summary>Makes the choices around a start; a start before today is moved to today.</summary>
    /// <param name="start">The start to choose.</param>
    /// <param name="today">Today, in the time zone of the device.</param>
    /// <returns>The choices.</returns>
    public static StartChoices Around(DateTimeOffset start, DateOnly today)
    {
        var local = start.ToLocalTime();
        var day = DateOnly.FromDateTime(local.DateTime);
        if (day < today)
            day = today;

        List<DateOnly> days = [.. Enumerable.Range(0, Math.Max(DaysAhead, day.DayNumber - today.DayNumber + 1)).Select(today.AddDays)];
        List<TimeOnly> times =
        [
            .. Enumerable.Range(0, ((LastHour - FirstHour) * 2) + 1).Select(half => new TimeOnly(FirstHour, 0).AddMinutes(half * 30))
        ];
        var halfHours = ((local.Hour * 60) + local.Minute) / 30;

        return new StartChoices(
            days, times, day.DayNumber - today.DayNumber, Math.Clamp(halfHours - (FirstHour * 2), 0, times.Count - 1));
    }

    /// <summary>Gives the start chosen in the pickers.</summary>
    /// <param name="dayIndex">The position of the day.</param>
    /// <param name="timeIndex">The position of the time.</param>
    /// <returns>The start, in the time zone of the device.</returns>
    public DateTimeOffset StartAt(int dayIndex, int timeIndex) =>
        new(_days[Math.Clamp(dayIndex, 0, _days.Count - 1)].ToDateTime(_times[Math.Clamp(timeIndex, 0, _times.Count - 1)]));
}
