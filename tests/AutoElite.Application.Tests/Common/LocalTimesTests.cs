using AutoElite.Domain.Entities;

namespace AutoElite.Application.Tests.Common;

public class LocalTimesTests
{
    private static readonly DateTime _utcTime = new(2026, 11, 12, 9, 0, 0, DateTimeKind.Utc);

    private readonly FakeBranchRepository _branches = new();
    private readonly FakeSchoolSettingsRepository _settings = new();

    private Task<DateTime> ToLocal(Guid? branchId) =>
        new LocalTimes(_branches, _settings).ToLocalAsync(_utcTime, branchId, TestContext.Current.CancellationToken);

    [Fact]
    public async Task ToLocal_RecordOfABranch_UsesTheTimeZoneOfTheBranch()
    {
        var branch = TestData.NewBranch();
        _branches.Seed(branch);

        var local = await ToLocal(branch.Id);

        local.Should().Be(new DateTime(2026, 11, 12, 14, 0, 0));
        local.Kind.Should().Be(DateTimeKind.Unspecified);
    }

    [Fact]
    public async Task ToLocal_RecordWithoutABranch_UsesTheTimeZoneOfTheSchool()
    {
        var settings = SchoolSettings.CreateDefault(Currency.Create("KZT").Value);
        settings.Update(settings.DefaultCurrency, "Asia/Novosibirsk", 10, 2, TimeSpan.FromHours(24), []);
        _settings.Settings = settings;

        var local = await ToLocal(null);

        local.Should().Be(new DateTime(2026, 11, 12, 16, 0, 0));
    }

    [Fact]
    public async Task ToLocal_NoSettingsYet_UsesTheDefaultTimeZone()
    {
        var local = await ToLocal(Guid.CreateVersion7());

        local.Should().Be(new DateTime(2026, 11, 12, 12, 0, 0));
    }
}
