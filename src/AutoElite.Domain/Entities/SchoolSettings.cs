using AutoElite.Domain.ValueObjects;

namespace AutoElite.Domain.Entities;

/// <summary>
/// The school-wide settings that tune business rules. Each value has a sensible default and can be changed
/// by an administrator.
/// </summary>
/// <remarks>
/// The school has exactly one settings record; the application layer creates it with <see cref="CreateDefault"/>
/// on first start and passes its values to the aggregates that need them.
/// </remarks>
public sealed class SchoolSettings : AuditableEntity
{
    /// <summary>The default number of latest practice grades the quality score averages.</summary>
    public const int DefaultQualityWindowSize = 10;

    /// <summary>The default rating at or below which a driving lesson rating is considered low.</summary>
    public const int DefaultLowRatingThreshold = 2;

    /// <summary>The time zone of a school that has not chosen one.</summary>
    public const string DefaultTimeZoneId = "Europe/Moscow";

    /// <summary>The most lesson reminders that can be configured.</summary>
    public const int MaxLessonReminders = 3;

    private const int MinQualityWindowSize = 1;
    private const int MinLowRatingThreshold = 1;
    private const int MaxLowRatingThreshold = 4;

    /// <summary>The default time before a lesson after which a student can no longer cancel it.</summary>
    public static readonly TimeSpan DefaultStudentCancellationNotice = TimeSpan.FromHours(24);

    /// <summary>The longest student cancellation notice that can be configured.</summary>
    public static readonly TimeSpan MaxStudentCancellationNotice = TimeSpan.FromHours(72);

    /// <summary>The shortest time before a lesson a reminder can be sent.</summary>
    public static readonly TimeSpan MinReminderLeadTime = TimeSpan.FromMinutes(15);

    /// <summary>The longest time before a lesson a reminder can be sent.</summary>
    public static readonly TimeSpan MaxReminderLeadTime = TimeSpan.FromDays(7);

    private static readonly TimeSpan[] _defaultLessonReminderLeadTimes = [TimeSpan.FromHours(24), TimeSpan.FromHours(2)];

    // Not read-only, so that the persistence layer can load the stored list into it.
    private List<TimeSpan> _lessonReminderLeadTimes = [];

    /// <summary>Gets the currency offered by default for new courses, contracts and payments.</summary>
    public Currency DefaultCurrency { get; private set; } = null!;

    /// <summary>
    /// Gets the IANA identifier of the school's time zone. Times of records not tied to a branch are shown in it;
    /// a branch has its own time zone.
    /// </summary>
    public string TimeZoneId { get; private set; } = null!;

    /// <summary>
    /// Gets how many of the latest practice grades the contract quality score averages, from 1 to
    /// <see cref="Contract.MaxStoredGrades"/>.
    /// </summary>
    public int QualityWindowSize { get; private set; }

    /// <summary>
    /// Gets the rating, from 1 to 4, at or below which a student's driving lesson rating notifies the responsible managers.
    /// </summary>
    public int LowRatingThreshold { get; private set; }

    /// <summary>
    /// Gets how long before a driving lesson the student may still cancel it, from zero to
    /// <see cref="MaxStudentCancellationNotice"/>; later only the instructor or a manager can cancel.
    /// </summary>
    public TimeSpan StudentCancellationNotice { get; private set; }

    /// <summary>
    /// Gets how long before a lesson the participants are reminded, longest first. Empty if reminders are turned off.
    /// </summary>
    public IReadOnlyList<TimeSpan> LessonReminderLeadTimes => _lessonReminderLeadTimes.AsReadOnly();

    private SchoolSettings(
        Guid id,
        Currency defaultCurrency,
        string timeZoneId,
        int qualityWindowSize,
        int lowRatingThreshold,
        TimeSpan studentCancellationNotice,
        IEnumerable<TimeSpan> lessonReminderLeadTimes) : base(id)
    {
        Apply(defaultCurrency, timeZoneId, qualityWindowSize, lowRatingThreshold, studentCancellationNotice, lessonReminderLeadTimes);
    }

    /// <summary>Required by EF Core for entity materialization.</summary>
    private SchoolSettings()
    {
    }

    /// <summary>Creates the settings with the default values.</summary>
    /// <param name="defaultCurrency">The school's main currency.</param>
    /// <returns>The settings with the default values, <see cref="DefaultTimeZoneId"/> among them, and the currency.</returns>
    public static SchoolSettings CreateDefault(Currency defaultCurrency)
    {
        return new SchoolSettings(
            Guid.CreateVersion7(),
            defaultCurrency,
            DefaultTimeZoneId,
            DefaultQualityWindowSize,
            DefaultLowRatingThreshold,
            DefaultStudentCancellationNotice,
            _defaultLessonReminderLeadTimes);
    }

    /// <summary>Replaces all settings at once. Nothing changes if any value is invalid.</summary>
    /// <param name="defaultCurrency">The currency offered by default; existing amounts keep their currency.</param>
    /// <param name="timeZoneId">The IANA identifier of the school's time zone, for example <c>Asia/Yekaterinburg</c>.</param>
    /// <param name="qualityWindowSize">The quality window, from 1 to <see cref="Contract.MaxStoredGrades"/>.</param>
    /// <param name="lowRatingThreshold">The low rating threshold, from 1 to 4.</param>
    /// <param name="studentCancellationNotice">The student cancellation notice, from zero to <see cref="MaxStudentCancellationNotice"/>.</param>
    /// <param name="lessonReminderLeadTimes">
    /// Up to <see cref="MaxLessonReminders"/> distinct lead times, each from <see cref="MinReminderLeadTime"/> to
    /// <see cref="MaxReminderLeadTime"/>; empty turns reminders off.
    /// </param>
    /// <returns>The first validation failure encountered, or success.</returns>
    public Result Update(
        Currency defaultCurrency,
        string? timeZoneId,
        int qualityWindowSize,
        int lowRatingThreshold,
        TimeSpan studentCancellationNotice,
        IReadOnlyCollection<TimeSpan> lessonReminderLeadTimes)
    {
        var timeZoneResult = Guard.IanaTimeZone(
            timeZoneId, DomainErrors.SchoolSettings.TimeZoneEmpty, DomainErrors.SchoolSettings.TimeZoneUnknown);
        if (timeZoneResult.IsFailure)
            return Result.Failure(timeZoneResult.Error);

        if (qualityWindowSize is < MinQualityWindowSize or > Contract.MaxStoredGrades)
            return Result.Failure(DomainErrors.SchoolSettings.QualityWindowOutOfRange);

        if (lowRatingThreshold is < MinLowRatingThreshold or > MaxLowRatingThreshold)
            return Result.Failure(DomainErrors.SchoolSettings.LowRatingThresholdOutOfRange);

        if (studentCancellationNotice < TimeSpan.Zero || studentCancellationNotice > MaxStudentCancellationNotice)
            return Result.Failure(DomainErrors.SchoolSettings.CancellationNoticeOutOfRange);

        var remindersResult = ValidateReminders(lessonReminderLeadTimes);
        if (remindersResult.IsFailure)
            return remindersResult;

        Apply(
            defaultCurrency, timeZoneResult.Value, qualityWindowSize, lowRatingThreshold, studentCancellationNotice,
            lessonReminderLeadTimes);

        return Result.Success();
    }

    private static Result ValidateReminders(IReadOnlyCollection<TimeSpan> leadTimes)
    {
        if (leadTimes.Count > MaxLessonReminders)
            return Result.Failure(DomainErrors.SchoolSettings.TooManyReminders);

        if (leadTimes.Any(leadTime => leadTime < MinReminderLeadTime || leadTime > MaxReminderLeadTime))
            return Result.Failure(DomainErrors.SchoolSettings.ReminderLeadTimeOutOfRange);

        if (leadTimes.Distinct().Count() != leadTimes.Count)
            return Result.Failure(DomainErrors.SchoolSettings.DuplicateReminder);

        return Result.Success();
    }

    private void Apply(
        Currency defaultCurrency,
        string timeZoneId,
        int qualityWindowSize,
        int lowRatingThreshold,
        TimeSpan studentCancellationNotice,
        IEnumerable<TimeSpan> lessonReminderLeadTimes)
    {
        DefaultCurrency = defaultCurrency;
        TimeZoneId = timeZoneId;
        QualityWindowSize = qualityWindowSize;
        LowRatingThreshold = lowRatingThreshold;
        StudentCancellationNotice = studentCancellationNotice;
        _lessonReminderLeadTimes.Clear();
        _lessonReminderLeadTimes.AddRange(lessonReminderLeadTimes.OrderDescending());
    }
}
