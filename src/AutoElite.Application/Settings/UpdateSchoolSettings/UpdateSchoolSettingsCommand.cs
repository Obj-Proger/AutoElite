namespace AutoElite.Application.Settings.UpdateSchoolSettings;

/// <summary>
/// Replaces the school settings. All values are given at once, as on the settings form; durations are in whole
/// minutes. The ranges are checked by the domain.
/// </summary>
/// <param name="DefaultCurrency">The ISO 4217 code of the currency offered by default; existing amounts keep theirs.</param>
/// <param name="TimeZoneId">
/// The IANA identifier of the school's time zone, for example <c>Asia/Yekaterinburg</c>; times of records not tied to
/// a branch are shown in it.
/// </param>
/// <param name="QualityWindowSize">How many of the latest practice grades the quality score averages, from 1 to 50.</param>
/// <param name="LowRatingThreshold">The low rating threshold, from 1 to 4.</param>
/// <param name="StudentCancellationNoticeMinutes">The student cancellation notice, from 0 to 72 hours.</param>
/// <param name="LessonReminderLeadTimeMinutes">Up to 3 distinct reminder lead times, each from 15 minutes to 7 days; empty turns reminders off.</param>
[RequireRoles(UserRole.Administrator)]
public sealed record UpdateSchoolSettingsCommand(
    string? DefaultCurrency,
    string? TimeZoneId,
    int QualityWindowSize,
    int LowRatingThreshold,
    int StudentCancellationNoticeMinutes,
    IReadOnlyList<int>? LessonReminderLeadTimeMinutes) : ICommand;
