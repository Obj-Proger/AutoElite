using AutoElite.Infrastructure.Persistence.Repositories;

namespace AutoElite.Infrastructure.Tests.Persistence;

public sealed class SchoolSettingsPersistenceTests(DatabaseFixture database) : IClassFixture<DatabaseFixture>
{
    private static readonly Currency _rubles = Currency.Create("RUB").Value;

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    /// <summary>Saves the default record unless an earlier test of the class already did.</summary>
    private async Task EnsureRecordAsync()
    {
        await using var context = database.CreateContext();
        var repository = new SchoolSettingsRepository(context);
        if (await repository.GetAsync(CancellationToken) is not null)
            return;

        repository.Add(SchoolSettings.CreateDefault(_rubles));
        await new UnitOfWork(context).SaveChangesAsync(CancellationToken);
    }

    [Fact]
    public async Task Save_ChangedSettings_StoresEveryValue()
    {
        await EnsureRecordAsync();
        var euros = Currency.Create("EUR").Value;

        await using (var context = database.CreateContext())
        {
            var settings = await new SchoolSettingsRepository(context).GetAsync(CancellationToken);
            settings!.Update(euros, "Asia/Yekaterinburg", 5, 3, TimeSpan.FromHours(12), [TimeSpan.FromHours(3), TimeSpan.FromDays(1)])
                .IsSuccess.Should().BeTrue();
            await new UnitOfWork(context).SaveChangesAsync(CancellationToken);
        }

        await using var reader = database.CreateContext();
        var loaded = await new SchoolSettingsRepository(reader).GetAsync(CancellationToken);

        loaded!.DefaultCurrency.Should().Be(euros);
        loaded.TimeZoneId.Should().Be("Asia/Yekaterinburg");
        loaded.QualityWindowSize.Should().Be(5);
        loaded.LowRatingThreshold.Should().Be(3);
        loaded.StudentCancellationNotice.Should().Be(TimeSpan.FromHours(12));
        loaded.LessonReminderLeadTimes.Should().Equal(TimeSpan.FromDays(1), TimeSpan.FromHours(3));
    }

    [Fact]
    public async Task Save_SecondRecord_ThrowsConcurrencyConflict()
    {
        await EnsureRecordAsync();
        await using var context = database.CreateContext();
        new SchoolSettingsRepository(context).Add(SchoolSettings.CreateDefault(_rubles));

        var save = () => new UnitOfWork(context).SaveChangesAsync(CancellationToken);

        await save.Should().ThrowAsync<ConcurrencyConflictException>();
    }
}
