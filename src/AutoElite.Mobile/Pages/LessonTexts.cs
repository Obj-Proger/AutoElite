using System.Globalization;

namespace AutoElite.Mobile.Pages;

/// <summary>The texts of a lesson that more than one page shows.</summary>
internal static class LessonTexts
{
    /// <summary>Gives the day and the time of a lesson in the time zone and the language of the device.</summary>
    /// <param name="start">The start.</param>
    /// <param name="end">The end.</param>
    /// <returns>Such as "Mon 16 Nov, 10:00 – 12:00".</returns>
    public static string TimeOf(DateTimeOffset start, DateTimeOffset end)
    {
        var culture = CultureInfo.CurrentCulture;
        var localStart = start.ToLocalTime();

        return string.Create(
            culture, $"{localStart.ToString("ddd d MMM", culture)}, {localStart:t} – {end.ToLocalTime():t}");
    }

    /// <summary>Joins the details of a lesson that it has, such as the instructor and the vehicle.</summary>
    /// <param name="parts">The details; those that are empty are left out.</param>
    /// <returns>The details, separated by a middle dot.</returns>
    public static string DetailsOf(params string?[] parts) =>
        string.Join(" · ", parts.Where(part => !string.IsNullOrWhiteSpace(part)));
}
