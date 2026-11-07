using AutoElite.Domain.Enums;
using AutoElite.Infrastructure.BackgroundJobs;
using AutoElite.Infrastructure.Storage;
using AutoElite.Infrastructure.Tests.Persistence;
using Microsoft.Extensions.Options;

namespace AutoElite.Infrastructure.Tests.Storage;

public sealed class FileCleanupJobTests(DatabaseFixture database) : IClassFixture<DatabaseFixture>, IDisposable
{
    private static readonly byte[] _png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 1, 2, 3, 4];

    private readonly string _root = Path.Combine(Path.GetTempPath(), $"autoelite-files-{Guid.NewGuid():N}");

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    public void Dispose()
    {
        if (Directory.Exists(_root))
            Directory.Delete(_root, recursive: true);

        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task RemoveOrphans_RemovesOldFilesWithoutADocumentAndHalfReceivedOnes()
    {
        var now = DateTime.UtcNow;
        var options = Options.Create(new FileStorageOptions { RootPath = _root });
        var storage = new LocalFileStorage(options);

        async Task<string> StoreAsync(DateTime storedAtUtc)
        {
            var key = $"documents/{Guid.NewGuid():N}";
            (await storage.SaveAsync(key, new MemoryStream(_png), "image/png", _png.Length, CancellationToken))
                .IsSuccess.Should().BeTrue();
            File.SetLastWriteTimeUtc(Path.Combine(_root, LocalFiles.DocumentsFolder, key[10..]), storedAtUtc);

            return key;
        }

        var named = await StoreAsync(now.AddDays(-3));
        var orphan = await StoreAsync(now.AddDays(-3));
        var young = await StoreAsync(now.AddHours(-1));
        var incoming = Path.Combine(_root, LocalFiles.IncomingFolder, "abandoned.part");
        await File.WriteAllBytesAsync(incoming, _png, CancellationToken);
        File.SetLastWriteTimeUtc(incoming, now.AddDays(-2));
        var uploader = TrainingRecords.Person("Белова");
        var document = Document.Attach(
                EntityReference.Create(EntityKind.User, uploader.Id).Value, "photo.png", "image/png", _png.Length, named, null,
                uploader.Id, now)
            .Value;
        await using (var context = database.CreateContext())
        {
            context.AddRange(uploader, document);
            await new UnitOfWork(context).SaveChangesAsync(CancellationToken);
        }

        await using var jobContext = database.CreateContext();
        var removed = await new FileCleanupJob(jobContext, storage, storage, TimeProvider.System, options)
            .RemoveOrphansAsync(CancellationToken);

        removed.Should().Be(1);
        (await storage.OpenReadAsync(orphan, CancellationToken)).Should().BeNull();
        await using (var kept = await storage.OpenReadAsync(named, CancellationToken))
            kept.Should().NotBeNull();

        await using (var kept = await storage.OpenReadAsync(young, CancellationToken))
            kept.Should().NotBeNull();

        File.Exists(incoming).Should().BeFalse();
    }
}
