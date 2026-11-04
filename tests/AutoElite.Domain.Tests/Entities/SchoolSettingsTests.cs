namespace AutoElite.Domain.Tests.Entities;

public class SchoolSettingsTests
{
    private static readonly Currency _rub = Currency.Create("RUB").Value;
    private static readonly Currency _kzt = Currency.Create("KZT").Value;
    private static readonly TimeSpan[] _reminders = [TimeSpan.FromHours(2), TimeSpan.FromDays(1)];

    private static Result Update(
        SchoolSettings settings,
        string? timeZoneId = "Asia/Yekaterinburg",
        int qualityWindowSize = 10,
        int lowRatingThreshold = 2,
        TimeSpan? studentCancellationNotice = null,
        TimeSpan[]? lessonReminderLeadTimes = null) =>
        settings.Update(
            _kzt,
            timeZoneId,
            qualityWindowSize,
            lowRatingThreshold,
            studentCancellationNotice ?? TimeSpan.FromHours(12),
            lessonReminderLeadTimes ?? _reminders);

    [Fact]
    public void CreateDefault_ReturnsTheDefaultValues()
    {
        var settings = SchoolSettings.CreateDefault(_rub);

        settings.Id.Should().NotBe(Guid.Empty);
        settings.DefaultCurrency.Should().Be(_rub);
        settings.TimeZoneId.Should().Be("Europe/Moscow");
        settings.QualityWindowSize.Should().Be(10);
        settings.LowRatingThreshold.Should().Be(2);
        settings.StudentCancellationNotice.Should().Be(TimeSpan.FromHours(24));
        settings.LessonReminderLeadTimes.Should().Equal(TimeSpan.FromHours(24), TimeSpan.FromHours(2));
    }

    [Fact]
    public void Update_ValidValues_ReplacesAllSettings()
    {
        var settings = SchoolSettings.CreateDefault(_rub);

        var result = settings.Update(_kzt, "Asia/Almaty", 20, 3, TimeSpan.FromHours(6), [TimeSpan.FromMinutes(30)]);

        result.IsSuccess.Should().BeTrue();
        settings.DefaultCurrency.Should().Be(_kzt);
        settings.TimeZoneId.Should().Be("Asia/Almaty");
        settings.QualityWindowSize.Should().Be(20);
        settings.LowRatingThreshold.Should().Be(3);
        settings.StudentCancellationNotice.Should().Be(TimeSpan.FromHours(6));
        settings.LessonReminderLeadTimes.Should().Equal(TimeSpan.FromMinutes(30));
    }

    [Fact]
    public void Update_RemindersInAnyOrder_StoresThemLongestFirst()
    {
        var settings = SchoolSettings.CreateDefault(_rub);

        Update(settings, lessonReminderLeadTimes: [TimeSpan.FromMinutes(15), TimeSpan.FromDays(7), TimeSpan.FromHours(3)]);

        settings.LessonReminderLeadTimes.Should().Equal(TimeSpan.FromDays(7), TimeSpan.FromHours(3), TimeSpan.FromMinutes(15));
    }

    [Fact]
    public void Update_NoReminders_TurnsRemindersOff()
    {
        var settings = SchoolSettings.CreateDefault(_rub);

        var result = Update(settings, lessonReminderLeadTimes: []);

        result.IsSuccess.Should().BeTrue();
        settings.LessonReminderLeadTimes.Should().BeEmpty();
    }

    [Fact]
    public void Update_TimeZoneWithSpaces_StoresItTrimmed()
    {
        var settings = SchoolSettings.CreateDefault(_rub);

        Update(settings, timeZoneId: "  Asia/Yekaterinburg ");

        settings.TimeZoneId.Should().Be("Asia/Yekaterinburg");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("   ")]
    public void Update_EmptyTimeZone_ReturnsTimeZoneEmptyErrorAndKeepsSettings(string? timeZoneId)
    {
        var settings = SchoolSettings.CreateDefault(_rub);

        var result = Update(settings, timeZoneId: timeZoneId);

        result.Error.Should().Be(DomainErrors.SchoolSettings.TimeZoneEmpty);
        settings.TimeZoneId.Should().Be(SchoolSettings.DefaultTimeZoneId);
        settings.DefaultCurrency.Should().Be(_rub);
    }

    [Theory]
    [InlineData("Mars/Olympus")]
    [InlineData("Russian Standard Time")]
    public void Update_UnknownOrWindowsTimeZone_ReturnsTimeZoneUnknownError(string timeZoneId)
    {
        var result = Update(SchoolSettings.CreateDefault(_rub), timeZoneId: timeZoneId);

        result.Error.Should().Be(DomainErrors.SchoolSettings.TimeZoneUnknown);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(50)]
    public void Update_QualityWindowAtBoundary_Succeeds(int window)
    {
        var settings = SchoolSettings.CreateDefault(_rub);

        var result = Update(settings, qualityWindowSize: window);

        result.IsSuccess.Should().BeTrue();
        settings.QualityWindowSize.Should().Be(window);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(51)]
    public void Update_QualityWindowOutOfRange_ReturnsErrorAndKeepsSettings(int window)
    {
        var settings = SchoolSettings.CreateDefault(_rub);

        var result = Update(settings, qualityWindowSize: window);

        result.Error.Should().Be(DomainErrors.SchoolSettings.QualityWindowOutOfRange);
        settings.QualityWindowSize.Should().Be(SchoolSettings.DefaultQualityWindowSize);
        settings.DefaultCurrency.Should().Be(_rub);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(5)]
    public void Update_LowRatingThresholdOutOfRange_ReturnsLowRatingThresholdOutOfRangeError(int threshold)
    {
        var result = Update(SchoolSettings.CreateDefault(_rub), lowRatingThreshold: threshold);

        result.Error.Should().Be(DomainErrors.SchoolSettings.LowRatingThresholdOutOfRange);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(72)]
    public void Update_CancellationNoticeAtBoundary_Succeeds(int hours)
    {
        var settings = SchoolSettings.CreateDefault(_rub);

        var result = Update(settings, studentCancellationNotice: TimeSpan.FromHours(hours));

        result.IsSuccess.Should().BeTrue();
        settings.StudentCancellationNotice.Should().Be(TimeSpan.FromHours(hours));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(73)]
    public void Update_CancellationNoticeOutOfRange_ReturnsCancellationNoticeOutOfRangeError(int hours)
    {
        var result = Update(SchoolSettings.CreateDefault(_rub), studentCancellationNotice: TimeSpan.FromHours(hours));

        result.Error.Should().Be(DomainErrors.SchoolSettings.CancellationNoticeOutOfRange);
    }

    [Fact]
    public void Update_MoreThanThreeReminders_ReturnsTooManyRemindersError()
    {
        var leadTimes = new[] { TimeSpan.FromHours(1), TimeSpan.FromHours(2), TimeSpan.FromHours(3), TimeSpan.FromHours(4) };

        var result = Update(SchoolSettings.CreateDefault(_rub), lessonReminderLeadTimes: leadTimes);

        result.Error.Should().Be(DomainErrors.SchoolSettings.TooManyReminders);
    }

    [Theory]
    [InlineData(14)]
    [InlineData(10081)]
    public void Update_ReminderLeadTimeOutOfRange_ReturnsReminderLeadTimeOutOfRangeError(int minutes)
    {
        var result = Update(SchoolSettings.CreateDefault(_rub), lessonReminderLeadTimes: [TimeSpan.FromMinutes(minutes)]);

        result.Error.Should().Be(DomainErrors.SchoolSettings.ReminderLeadTimeOutOfRange);
    }

    [Fact]
    public void Update_RepeatedReminder_ReturnsDuplicateReminderErrorAndKeepsReminders()
    {
        var settings = SchoolSettings.CreateDefault(_rub);

        var result = Update(settings, lessonReminderLeadTimes: [TimeSpan.FromHours(2), TimeSpan.FromMinutes(120)]);

        result.Error.Should().Be(DomainErrors.SchoolSettings.DuplicateReminder);
        settings.LessonReminderLeadTimes.Should().Equal(TimeSpan.FromHours(24), TimeSpan.FromHours(2));
    }
}
