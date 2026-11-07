using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using AutoElite.Application.Common;
using AutoElite.Infrastructure.Storage;
using Microsoft.Extensions.Options;

namespace AutoElite.Infrastructure.Tests.Storage;

/// <summary>
/// A bucket of its own for one test class in the storage of the tests (<see cref="TestS3"/>), emptied and removed
/// when the class has run.
/// </summary>
public sealed class S3BucketFixture : IAsyncLifetime
{
    /// <summary>Gets the settings of the storage with the bucket.</summary>
    public FileStorageOptions Options { get; } = new() { Provider = FileStorageProvider.S3 };

    /// <summary>Gets a value indicating whether there is a storage; without one the tests are skipped.</summary>
    public bool IsAvailable { get; private set; }

    /// <summary>Skips the calling test when there is no storage.</summary>
    public void SkipIfMissing()
    {
        if (!IsAvailable)
            Assert.Skip(TestS3.MissingReason);
    }

    /// <inheritdoc />
    public async ValueTask InitializeAsync()
    {
        var server = await TestS3.GetServerAsync();
        if (server is null)
            return;

        IsAvailable = true;
        var settings = Options.S3;
        settings.ServiceUrl = server.ServiceUrl;
        settings.Region = server.Region;
        settings.AccessKey = server.AccessKey;
        settings.SecretKey = server.SecretKey;
        settings.BucketName = $"autoelite-test-{Guid.NewGuid():N}";
        settings.StagingPath = Path.Combine(Path.GetTempPath(), $"autoelite-incoming-{Guid.NewGuid():N}");

        using var client = Client();

        // A container that has just started takes a moment before it accepts buckets.
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                await client.PutBucketAsync(settings.BucketName);
                break;
            }
            catch (AmazonS3Exception) when (TestS3.IsContainer && attempt < 30)
            {
                await Task.Delay(TimeSpan.FromSeconds(1));
            }
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (!IsAvailable)
            return;

        using var client = Client();
        var listing = await client.ListObjectsV2Async(new ListObjectsV2Request { BucketName = Options.S3.BucketName });
        foreach (var stored in listing.S3Objects ?? [])
            await client.DeleteObjectAsync(Options.S3.BucketName, stored.Key);

        await client.DeleteBucketAsync(Options.S3.BucketName);
        if (Directory.Exists(Options.S3.StagingPath))
            Directory.Delete(Options.S3.StagingPath, recursive: true);
    }

    private AmazonS3Client Client() =>
        new(
            new BasicAWSCredentials(Options.S3.AccessKey, Options.S3.SecretKey),
            new AmazonS3Config
            {
                ServiceURL = Options.S3.ServiceUrl,
                ForcePathStyle = true,
                AuthenticationRegion = Options.S3.Region
            });
}

public sealed class S3FileStorageTests(S3BucketFixture storage) : IClassFixture<S3BucketFixture>
{
    private static readonly byte[] _png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 1, 2, 3, 4, 5, 6, 7, 8];

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    private static string NewKey() => $"documents/{Guid.NewGuid():N}";

    private S3FileStorage Storage() => new(Options.Create(storage.Options));

    [Fact]
    public async Task Save_ThenOpenListAndDelete_WorkOnTheBucket()
    {
        storage.SkipIfMissing();
        using var files = Storage();
        var key = NewKey();

        var saved = await files.SaveAsync(key, new MemoryStream(_png), "image/png", _png.Length, CancellationToken);
        byte[] read;
        await using (var stream = await files.OpenReadAsync(key, CancellationToken))
        {
            using var copy = new MemoryStream();
            await stream!.CopyToAsync(copy, CancellationToken);
            read = copy.ToArray();
        }

        var listedLater = await ListAsync(files, DateTime.UtcNow.AddMinutes(5));
        var listedEarlier = await ListAsync(files, DateTime.UtcNow.AddHours(-1));
        await files.DeleteAsync(key, CancellationToken);
        await files.DeleteAsync(key, CancellationToken);

        saved.IsSuccess.Should().BeTrue();
        read.Should().Equal(_png);
        listedLater.Should().Contain(key);
        listedEarlier.Should().NotContain(key);
        (await files.OpenReadAsync(key, CancellationToken)).Should().BeNull();
    }

    [Fact]
    public async Task Save_ContentOfAnotherSize_UploadsNothing()
    {
        storage.SkipIfMissing();
        using var files = Storage();
        var key = NewKey();

        var saved = await files.SaveAsync(key, new MemoryStream(_png), "image/png", _png.Length + 1, CancellationToken);

        saved.Error.Should().Be(ApplicationErrors.Document.ContentMismatch);
        (await files.OpenReadAsync(key, CancellationToken)).Should().BeNull();
        Directory.GetFiles(storage.Options.S3.StagingFolder).Should().BeEmpty();
    }

    [Fact]
    public void Options_S3WithoutItsSettings_AreRefused()
    {
        var missing = new FileStorageOptions { Provider = FileStorageProvider.S3 };
        var complete = new FileStorageOptions { Provider = FileStorageProvider.S3 };
        complete.S3.ServiceUrl = "https://s3.example.com";
        complete.S3.BucketName = "autoelite";
        complete.S3.AccessKey = "access";
        complete.S3.SecretKey = "secret";

        missing.Problems().Should().HaveCount(3);
        complete.Problems().Should().BeEmpty();
    }

    private static async Task<List<string>> ListAsync(S3FileStorage files, DateTime storedBeforeUtc)
    {
        var keys = new List<string>();
        await foreach (var key in files.ListKeysAsync(storedBeforeUtc, CancellationToken))
            keys.Add(key);

        return keys;
    }
}
