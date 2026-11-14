using System.Text;
using AutoElite.Client.Offline;
using AutoElite.Client.Offline.Actions;
using Microsoft.Data.Sqlite;
using LeadRequest = AutoElite.Client.Api.Models.SubmitLeadRequestCommand;
using Skill = AutoElite.Client.Api.Models.DrivingSkill;

namespace AutoElite.API.Tests.Client.Offline;

/// <summary>The offline data on the device, which outlives the application and belongs to one user at a time.</summary>
public sealed class OfflineDatabaseTests : IDisposable
{
    private readonly string _path = Path.Combine(Path.GetTempPath(), $"autoelite-offline-{Guid.NewGuid():N}.db3");
    private readonly MemorySecretStore _secrets = new();

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    private static Task<List<LeadRequest>?> Unreachable(CancellationToken cancellationToken) =>
        Task.FromException<List<LeadRequest>?>(new HttpRequestException("The device is offline."));

    private static List<LeadRequest> Leads() =>
    [
        new() { ContactName = "Олег Смирнов", Phone = "+79980000001" },
        new() { ContactName = "Анна Петрова", Phone = "+79980000002" }
    ];

    private static Task<Cached<IReadOnlyList<LeadRequest>>> KeepAsync(OfflineCache cache, string key) =>
        cache.GetListAsync(
            key, _ => Task.FromResult<List<LeadRequest>?>(Leads()), LeadRequest.CreateFromDiscriminatorValue, CancellationToken);

    private static Task<Cached<IReadOnlyList<LeadRequest>>> ReadOfflineAsync(OfflineCache cache, string key) =>
        cache.GetListAsync(key, Unreachable, LeadRequest.CreateFromDiscriminatorValue, CancellationToken);

    private async Task ExecuteAsync(string sql)
    {
        await using var connection = new SqliteConnection($"Data Source={_path};Pooling=False");
        await connection.OpenAsync(CancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        await command.ExecuteNonQueryAsync(CancellationToken);
    }

    /// <inheritdoc />
    public void Dispose() => File.Delete(_path);

    [Fact]
    public async Task ListReadOnline_IsGivenFromTheDeviceWithoutANetwork()
    {
        using var database = new OfflineDatabase(_path, _secrets);
        var cache = new OfflineCache(database, TimeProvider.System);

        var online = await KeepAsync(cache, "leads");
        var offline = await ReadOfflineAsync(cache, "leads");

        online.IsFromCache.Should().BeFalse();
        offline.IsFromCache.Should().BeTrue();
        offline.Value.Select(lead => lead.Phone).Should().Equal("+79980000001", "+79980000002");
        offline.Value.Select(lead => lead.ContactName).Should().Equal("Олег Смирнов", "Анна Петрова");
    }

    [Fact]
    public async Task QueuedActions_OutliveTheApplication()
    {
        var booking = new BookDrivingSlot(Guid.NewGuid(), Guid.NewGuid());
        var cancellation = new CancelDrivingBooking(Guid.NewGuid(), "Заболел");
        var noShow = new MarkDrivingNoShow(Guid.NewGuid());
        var completion = new CompleteDrivingLesson(
            Guid.NewGuid(),
            1.75m,
            "Уверенно на перекрёстках",
            [new DrivingSkillGrade(Skill.CityDriving, 5), new DrivingSkillGrade(Skill.GearShifting, 3)]);
        var attendance = new MarkTheoryAttendance(
            Guid.NewGuid(),
            [new TheoryAttendanceMark(Guid.NewGuid(), IsPresent: true), new TheoryAttendanceMark(Guid.NewGuid(), false, "Болел")]);
        var message = new SendChatMessage(Guid.NewGuid(), "Буду через 5 минут");
        var rating = new RateDrivingLesson(Guid.NewGuid(), 5, "Спокойно и понятно");
        using (var database = new OfflineDatabase(_path, _secrets))
        {
            var queue = new OfflineQueue(database, TimeProvider.System);
            await queue.EnqueueAsync(booking, CancellationToken);
            await queue.EnqueueAsync(cancellation, CancellationToken);
            await queue.EnqueueAsync(noShow, CancellationToken);
            await queue.EnqueueAsync(completion, CancellationToken);
            await queue.EnqueueAsync(attendance, CancellationToken);
            await queue.EnqueueAsync(message, CancellationToken);
            await queue.EnqueueAsync(rating, CancellationToken);
        }

        using var restarted = new OfflineDatabase(_path, _secrets);
        var pending = await new OfflineQueue(restarted, TimeProvider.System).ListAsync(CancellationToken);

        pending.Select(action => action.Action).Should().Equal(booking, cancellation, noShow, completion, attendance, message, rating);
    }

    [Fact]
    public async Task ActionThisVersionCannotRead_IsDropped()
    {
        using var database = new OfflineDatabase(_path, _secrets);
        var queue = new OfflineQueue(database, TimeProvider.System);
        var booking = new BookDrivingSlot(Guid.NewGuid(), Guid.NewGuid());
        await queue.EnqueueAsync(booking, CancellationToken);
        await database.RunAsync(
            connection => OfflineDatabase.Execute(
                connection,
                "INSERT INTO pending_actions (content, queued_at) VALUES ($content, 0)",
                ("$content", database.Encrypt("""{"kind":"driving-slot.teleport"}""", OfflineQueue.Place))),
            CancellationToken);

        var pending = await queue.ListAsync(CancellationToken);
        var again = await queue.ListAsync(CancellationToken);

        pending.Select(action => action.Action).Should().Equal(booking);
        again.Should().HaveCount(1);
    }

    [Fact]
    public async Task DataOfAnotherUser_IsForgottenWhenTheyClaimTheDevice()
    {
        using var database = new OfflineDatabase(_path, _secrets);
        var queue = new OfflineQueue(database, TimeProvider.System);
        var (first, second) = (Guid.NewGuid(), Guid.NewGuid());

        var claimedFirst = await database.ClaimAsync(first, CancellationToken);
        await queue.EnqueueAsync(new BookDrivingSlot(Guid.NewGuid(), Guid.NewGuid()), CancellationToken);
        var claimedAgain = await database.ClaimAsync(first, CancellationToken);
        var keptForTheSameUser = await queue.ListAsync(CancellationToken);
        var claimedSecond = await database.ClaimAsync(second, CancellationToken);
        var keptForAnotherUser = await queue.ListAsync(CancellationToken);

        claimedFirst.Should().BeFalse();
        claimedAgain.Should().BeFalse();
        keptForTheSameUser.Should().HaveCount(1);
        claimedSecond.Should().BeTrue();
        keptForAnotherUser.Should().BeEmpty();
    }

    [Fact]
    public async Task SignOut_ForgetsEverything()
    {
        using var database = new OfflineDatabase(_path, _secrets);
        var queue = new OfflineQueue(database, TimeProvider.System);
        await database.ClaimAsync(Guid.NewGuid(), CancellationToken);
        await queue.EnqueueAsync(new MarkDrivingNoShow(Guid.NewGuid(), "Не пришёл"), CancellationToken);

        var key = await _secrets.GetAsync(OfflineEncryption.KeyName);

        await database.ForgetAsync(CancellationToken);
        var forgottenKey = await _secrets.GetAsync(OfflineEncryption.KeyName);
        var pending = await queue.ListAsync(CancellationToken);

        key.Should().NotBeNull();
        forgottenKey.Should().BeNull();
        pending.Should().BeEmpty();
        (await _secrets.GetAsync(OfflineEncryption.KeyName)).Should().NotBeNull().And.NotBe(key);
    }

    [Fact]
    public async Task KeptData_CannotBeReadFromTheFileAlone()
    {
        using (var database = new OfflineDatabase(_path, _secrets))
        {
            await KeepAsync(new OfflineCache(database, TimeProvider.System), "leads");
            await new OfflineQueue(database, TimeProvider.System)
                .EnqueueAsync(new SendChatMessage(Guid.NewGuid(), "Буду через 5 минут"), CancellationToken);
        }

        var file = await File.ReadAllBytesAsync(_path, CancellationToken);

        foreach (var secret in new[] { "+79980000001", "Олег Смирнов", "Буду через 5 минут", "chat.message" })
            file.AsSpan().IndexOf(Encoding.UTF8.GetBytes(secret)).Should().Be(-1, $"'{secret}' is kept encrypted");
    }

    [Fact]
    public async Task DataWhoseKeyIsLost_IsForgotten()
    {
        using (var database = new OfflineDatabase(_path, _secrets))
        {
            await KeepAsync(new OfflineCache(database, TimeProvider.System), "leads");
            await new OfflineQueue(database, TimeProvider.System)
                .EnqueueAsync(new MarkDrivingNoShow(Guid.NewGuid()), CancellationToken);
        }

        using var restored = new OfflineDatabase(_path, new MemorySecretStore());
        var read = () => ReadOfflineAsync(new OfflineCache(restored, TimeProvider.System), "leads");

        await read.Should().ThrowAsync<HttpRequestException>();
        (await new OfflineQueue(restored, TimeProvider.System).ListAsync(CancellationToken)).Should().BeEmpty();
    }

    [Fact]
    public async Task AnswerMovedToAnotherKey_CannotBeRead()
    {
        using var database = new OfflineDatabase(_path, _secrets);
        var cache = new OfflineCache(database, TimeProvider.System);
        await KeepAsync(cache, "leads");

        await ExecuteAsync("INSERT INTO snapshots (key, content, saved_at) SELECT 'contacts', content, saved_at FROM snapshots");
        var moved = () => ReadOfflineAsync(cache, "contacts");

        await moved.Should().ThrowAsync<HttpRequestException>();
        (await ReadOfflineAsync(cache, "leads")).Value.Should().HaveCount(2);
    }

    [Fact]
    public async Task DatabaseOfTheFirstVersion_IsStartedAfresh()
    {
        await ExecuteAsync("""
            CREATE TABLE settings (key TEXT NOT NULL PRIMARY KEY, value TEXT NOT NULL) WITHOUT ROWID;
            CREATE TABLE snapshots (key TEXT NOT NULL PRIMARY KEY, content TEXT NOT NULL, saved_at INTEGER NOT NULL) WITHOUT ROWID;
            CREATE TABLE pending_actions (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                content TEXT NOT NULL,
                queued_at INTEGER NOT NULL,
                attempts INTEGER NOT NULL DEFAULT 0,
                rejected_with_status INTEGER NULL);
            INSERT INTO snapshots (key, content, saved_at) VALUES ('leads', '[{"phone":"+79980000001"}]', 0);
            INSERT INTO pending_actions (content, queued_at) VALUES ('{"kind":"driving-slot.no-show","slotId":"%"}', 0);
            PRAGMA user_version = 1;
            """.Replace("%", Guid.NewGuid().ToString(), StringComparison.Ordinal));

        using var database = new OfflineDatabase(_path, _secrets);
        var read = () => ReadOfflineAsync(new OfflineCache(database, TimeProvider.System), "leads");

        await read.Should().ThrowAsync<HttpRequestException>();
        (await new OfflineQueue(database, TimeProvider.System).ListAsync(CancellationToken)).Should().BeEmpty();
    }
}
