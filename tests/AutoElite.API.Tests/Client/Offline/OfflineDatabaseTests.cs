using AutoElite.Client.Offline;
using AutoElite.Client.Offline.Actions;
using Microsoft.Data.Sqlite;
using LeadRequest = AutoElite.Client.Api.Models.SubmitLeadRequestCommand;

namespace AutoElite.API.Tests.Client.Offline;

/// <summary>The offline data on the device, which outlives the application and belongs to one user at a time.</summary>
public sealed class OfflineDatabaseTests : IDisposable
{
    private readonly string _path = Path.Combine(Path.GetTempPath(), $"autoelite-offline-{Guid.NewGuid():N}.db3");

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    private static Task<List<LeadRequest>?> Unreachable(CancellationToken cancellationToken) =>
        Task.FromException<List<LeadRequest>?>(new HttpRequestException("The device is offline."));

    /// <inheritdoc />
    public void Dispose() => File.Delete(_path);

    [Fact]
    public async Task ListReadOnline_IsGivenFromTheDeviceWithoutANetwork()
    {
        using var database = new OfflineDatabase(_path);
        var cache = new OfflineCache(database, TimeProvider.System);
        List<LeadRequest> leads =
        [
            new() { ContactName = "Олег Смирнов", Phone = "+79980000001" },
            new() { ContactName = "Анна Петрова", Phone = "+79980000002" }
        ];

        var online = await cache.GetListAsync(
            "leads", _ => Task.FromResult<List<LeadRequest>?>(leads), LeadRequest.CreateFromDiscriminatorValue, CancellationToken);
        var offline = await cache.GetListAsync("leads", Unreachable, LeadRequest.CreateFromDiscriminatorValue, CancellationToken);

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
        var completion = new CompleteDrivingLesson(Guid.NewGuid(), 1.75m, "Уверенно на перекрёстках");
        using (var database = new OfflineDatabase(_path))
        {
            var queue = new OfflineQueue(database, TimeProvider.System);
            await queue.EnqueueAsync(booking, CancellationToken);
            await queue.EnqueueAsync(cancellation, CancellationToken);
            await queue.EnqueueAsync(noShow, CancellationToken);
            await queue.EnqueueAsync(completion, CancellationToken);
        }

        using var restarted = new OfflineDatabase(_path);
        var pending = await new OfflineQueue(restarted, TimeProvider.System).ListAsync(CancellationToken);

        pending.Select(action => action.Action).Should().Equal(booking, cancellation, noShow, completion);
    }

    [Fact]
    public async Task ActionThisVersionCannotRead_IsDropped()
    {
        using var database = new OfflineDatabase(_path);
        var queue = new OfflineQueue(database, TimeProvider.System);
        var booking = new BookDrivingSlot(Guid.NewGuid(), Guid.NewGuid());
        await queue.EnqueueAsync(booking, CancellationToken);
        await using (var connection = new SqliteConnection($"Data Source={_path};Pooling=False"))
        {
            await connection.OpenAsync(CancellationToken);
            await using var command = connection.CreateCommand();
            command.CommandText = """
                INSERT INTO pending_actions (content, queued_at) VALUES ('{"kind":"driving-slot.teleport"}', 0)
                """;
            await command.ExecuteNonQueryAsync(CancellationToken);
        }

        var pending = await queue.ListAsync(CancellationToken);
        var again = await queue.ListAsync(CancellationToken);

        pending.Select(action => action.Action).Should().Equal(booking);
        again.Should().HaveCount(1);
    }

    [Fact]
    public async Task DataOfAnotherUser_IsForgottenWhenTheyClaimTheDevice()
    {
        using var database = new OfflineDatabase(_path);
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
        using var database = new OfflineDatabase(_path);
        var queue = new OfflineQueue(database, TimeProvider.System);
        await database.ClaimAsync(Guid.NewGuid(), CancellationToken);
        await queue.EnqueueAsync(new MarkDrivingNoShow(Guid.NewGuid(), "Не пришёл"), CancellationToken);

        await database.ForgetAsync(CancellationToken);

        (await queue.ListAsync(CancellationToken)).Should().BeEmpty();
    }
}
