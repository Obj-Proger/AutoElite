using AutoElite.Application.Settings;
using AutoElite.Application.Settings.GetSchoolSettings;
using AutoElite.Application.Settings.UpdateSchoolSettings;
using AutoElite.Domain.Entities;

namespace AutoElite.Application.Tests.Settings;

public class SchoolSettingsHandlerTests
{
    private readonly FakeSchoolSettingsRepository _repository = new();

    private static readonly UpdateSchoolSettingsCommand _command = new("KZT", "Asia/Almaty", 20, 3, 360, [30, 1440]);

    private Task<Result<SchoolSettingsDetails>> Get() =>
        new GetSchoolSettingsQueryHandler(new SchoolSettingsProvider(_repository))
            .Handle(new GetSchoolSettingsQuery(), TestContext.Current.CancellationToken);

    private Task<Result> Update(UpdateSchoolSettingsCommand command) =>
        new UpdateSchoolSettingsCommandHandler(new SchoolSettingsProvider(_repository))
            .Handle(command, TestContext.Current.CancellationToken);

    [Fact]
    public async Task Get_NoRecordYet_ReturnsTheDefaults()
    {
        var result = await Get();

        var details = result.Value;
        details.DefaultCurrency.Should().Be("RUB");
        details.TimeZoneId.Should().Be("Europe/Moscow");
        details.QualityWindowSize.Should().Be(10);
        details.LowRatingThreshold.Should().Be(2);
        details.StudentCancellationNoticeMinutes.Should().Be(1440);
        details.LessonReminderLeadTimeMinutes.Should().Equal(1440, 120);
    }

    [Fact]
    public async Task Get_ExistingRecord_ReturnsItsValuesWithoutAddingAnother()
    {
        var settings = SchoolSettings.CreateDefault(Currency.Create("USD").Value);
        _repository.Settings = settings;

        var result = await Get();

        result.Value.DefaultCurrency.Should().Be("USD");
        _repository.AddCount.Should().Be(0);
    }

    [Fact]
    public async Task Update_ExistingRecord_AppliesEveryValue()
    {
        _repository.Settings = SchoolSettings.CreateDefault(Currency.Create("RUB").Value);

        var result = await Update(_command);

        result.IsSuccess.Should().BeTrue();
        var settings = _repository.Settings!;
        settings.DefaultCurrency.Code.Should().Be("KZT");
        settings.TimeZoneId.Should().Be("Asia/Almaty");
        settings.QualityWindowSize.Should().Be(20);
        settings.LowRatingThreshold.Should().Be(3);
        settings.StudentCancellationNotice.Should().Be(TimeSpan.FromHours(6));
        settings.LessonReminderLeadTimes.Should().Equal(TimeSpan.FromDays(1), TimeSpan.FromMinutes(30));
        _repository.AddCount.Should().Be(0);
    }

    [Fact]
    public async Task Update_NoRecordYet_CreatesItWithTheValues()
    {
        var result = await Update(_command);

        result.IsSuccess.Should().BeTrue();
        _repository.AddCount.Should().Be(1);
        _repository.Settings!.QualityWindowSize.Should().Be(20);
    }

    [Fact]
    public async Task Update_NoReminders_TurnsThemOff()
    {
        await Update(_command with { LessonReminderLeadTimeMinutes = null });

        _repository.Settings!.LessonReminderLeadTimes.Should().BeEmpty();
    }

    [Fact]
    public async Task Update_ValueOutOfRange_ReturnsTheDomainErrorAndKeepsTheSettings()
    {
        _repository.Settings = SchoolSettings.CreateDefault(Currency.Create("RUB").Value);

        var result = await Update(_command with { QualityWindowSize = 51 });

        result.Error.Should().Be(DomainErrors.SchoolSettings.QualityWindowOutOfRange);
        _repository.Settings!.QualityWindowSize.Should().Be(SchoolSettings.DefaultQualityWindowSize);
        _repository.Settings!.DefaultCurrency.Code.Should().Be("RUB");
    }

    [Fact]
    public async Task Update_UnknownTimeZone_ReturnsTimeZoneUnknown()
    {
        var result = await Update(_command with { TimeZoneId = "Mars/Olympus" });

        result.Error.Should().Be(DomainErrors.SchoolSettings.TimeZoneUnknown);
    }

    [Fact]
    public async Task Update_NegativeNotice_ReturnsCancellationNoticeOutOfRange()
    {
        var result = await Update(_command with { StudentCancellationNoticeMinutes = -1 });

        result.Error.Should().Be(DomainErrors.SchoolSettings.CancellationNoticeOutOfRange);
    }

    [Theory]
    [InlineData(null, "Currency.Empty")]
    [InlineData("rubles", "Currency.InvalidFormat")]
    [InlineData("XXX", "Currency.NotSupported")]
    public void Validator_InvalidCurrency_ReportsTheCurrencyError(string? currency, string code)
    {
        var result = new UpdateSchoolSettingsCommandValidator().Validate(_command with { DefaultCurrency = currency });

        result.Errors.Should().ContainSingle().Which.ErrorCode.Should().Be(code);
    }
}
