using System.Globalization;

namespace AutoElite.Mobile.Schedule;

/// <summary>
/// The weeks of the schedule in the time zone and the calendar of the device: the week starts on the day its culture
/// starts a week, Monday in Russia, and each week is kept on the device under a key of its own.
/// </summary>
internal static class ScheduleWeek
{
    /// <summary>The number of days of a week.</summary>
    public const int Days = 7;

    /// <summary>Gives the first day of the week a day is in.</summary>
    /// <param name="day">The day.</param>
    /// <returns>The first day of its week.</returns>
    public static DateOnly FirstDayOf(DateOnly day)
    {
        var first = CultureInfo.CurrentCulture.DateTimeFormat.FirstDayOfWeek;

        return day.AddDays(-(((int)day.DayOfWeek - (int)first + Days) % Days));
    }

    /// <summary>Gives the moment a day starts on the device, in UTC.</summary>
    /// <param name="day">The day.</param>
    /// <returns>Its local midnight.</returns>
    public static DateTimeOffset StartOf(DateOnly day) =>
        new(TimeZoneInfo.ConvertTimeToUtc(day.ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified), TimeZoneInfo.Local));

    /// <summary>Gives the key under which a week of a list is kept on the device.</summary>
    /// <param name="list">The key of the list, such as me/driving-lessons.</param>
    /// <param name="firstDay">The first day of the week.</param>
    /// <returns>The key of the week.</returns>
    public static string KeyOf(string list, DateOnly firstDay) =>
        string.Create(CultureInfo.InvariantCulture, $"{list}/week/{firstDay:yyyy-MM-dd}");
}
