using System.Net;
using System.Runtime.CompilerServices;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using AutoElite.Application.Common;
using Microsoft.Extensions.Options;

namespace AutoElite.Infrastructure.Storage;

/// <summary>
/// Keeps the files of documents in a bucket of an S3-compatible storage, under their keys (D-069). A file is received
/// into a local folder and checked there, and only a checked file is uploaded, so the bucket never holds a rejected
/// one. Checksums are sent only when an operation asks for them, since some compatible storages refuse the checksums
/// that the SDK sends by default.
/// </summary>
internal sealed class S3FileStorage : IDocumentFileStore, IDisposable
{
    private readonly S3StorageOptions _settings;
    private readonly AmazonS3Client _client;

    /// <summary>Initializes a new instance of the <see cref="S3FileStorage"/> class.</summary>
    /// <param name="options">The settings, with the bucket.</param>
    public S3FileStorage(IOptions<FileStorageOptions> options)
    {
        _settings = options.Value.S3;
        _client = new AmazonS3Client(
            new BasicAWSCredentials(_settings.AccessKey, _settings.SecretKey),
            new AmazonS3Config
            {
                ServiceURL = _settings.ServiceUrl,
                AuthenticationRegion = _settings.Region,
                ForcePathStyle = _settings.ForcePathStyle,
                RequestChecksumCalculation = RequestChecksumCalculation.WHEN_REQUIRED,
                ResponseChecksumValidation = ResponseChecksumValidation.WHEN_REQUIRED
            });
    }

    /// <inheritdoc />
    public async Task<Result> SaveAsync(
        string storageKey, Stream content, string contentType, long sizeBytes, CancellationToken cancellationToken = default)
    {
        LocalFiles.EnsureValidKey(storageKey);
        await using var staged = await StagedFile.ReceiveAsync(
            _settings.StagingFolder, content, contentType, sizeBytes, cancellationToken);
        if (!staged.IsValid)
            return Result.Failure(ApplicationErrors.Document.ContentMismatch);

        await _client.PutObjectAsync(
            new PutObjectRequest
            {
                BucketName = _settings.BucketName,
                Key = storageKey,
                FilePath = staged.Path,
                ContentType = contentType
            },
            cancellationToken);

        return Result.Success();
    }

    /// <inheritdoc />
    public async Task<Stream?> OpenReadAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        LocalFiles.EnsureValidKey(storageKey);
        try
        {
            // Disposing the stream releases the response and its connection.
            var response = await _client.GetObjectAsync(_settings.BucketName, storageKey, cancellationToken);

            return response.ResponseStream;
        }
        catch (AmazonS3Exception exception) when (exception.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    /// <inheritdoc />
    public async Task DeleteAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        LocalFiles.EnsureValidKey(storageKey);

        // Deleting a missing object succeeds in S3, so a repeated call is harmless.
        await _client.DeleteObjectAsync(_settings.BucketName, storageKey, cancellationToken);
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<string> ListKeysAsync(
        DateTime storedBeforeUtc, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var request = new ListObjectsV2Request
        {
            BucketName = _settings.BucketName,
            Prefix = $"{LocalFiles.DocumentsFolder}/"
        };
        ListObjectsV2Response response;
        do
        {
            response = await _client.ListObjectsV2Async(request, cancellationToken);
            foreach (var stored in response.S3Objects ?? [])
            {
                if (stored.LastModified is { } modified && modified.ToUniversalTime() < storedBeforeUtc)
                    yield return stored.Key;
            }

            request.ContinuationToken = response.NextContinuationToken;
        }
        while (response.IsTruncated == true);
    }

    /// <inheritdoc />
    public Task RemoveAbandonedIncomingAsync(DateTime storedBeforeUtc, CancellationToken cancellationToken)
    {
        LocalFiles.RemoveAbandonedIncoming(_settings.StagingFolder, storedBeforeUtc, cancellationToken);

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public void Dispose() => _client.Dispose();
}
