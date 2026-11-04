namespace AutoElite.Application.Settings;

/// <summary>The school settings as the settings page shows them. Durations are in whole minutes.</summary>
/// <param name="DefaultCurrency">The ISO 4217 code of the currency offered by default.</param>
/// <param name="QualityWindowSize">How many of the latest practice grades the quality score averages.</param>
/// <param name="LowRatingThreshold">The rating at or below which a lesson rating notifies the managers.</param>
/// <param name="StudentCancellationNoticeMinutes">How long before a lesson a student may still cancel it.</param>
/// <param name="LessonReminderLeadTimeMinutes">How long before a lesson the participants are reminded, longest first.</param>
public sealed record SchoolSettingsDetails(
    string DefaultCurrency,
    int QualityWindowSize,
    int LowRatingThreshold,
    int StudentCancellationNoticeMinutes,
    IReadOnlyList<int> LessonReminderLeadTimeMinutes)
{
    /// <summary>Copies the values of the settings record.</summary>
    /// <param name="settings">The settings record.</param>
    public static SchoolSettingsDetails From(SchoolSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        return new SchoolSettingsDetails(
            settings.DefaultCurrency.Code,
            settings.QualityWindowSize,
            settings.LowRatingThreshold,
            (int)settings.StudentCancellationNotice.TotalMinutes,
            [.. settings.LessonReminderLeadTimes.Select(leadTime => (int)leadTime.TotalMinutes)]);
    }
}
