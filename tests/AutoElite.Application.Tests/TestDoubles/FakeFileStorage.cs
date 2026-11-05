using AutoElite.Application.Documents;

namespace AutoElite.Application.Tests.TestDoubles;

/// <summary>Keeps files in memory; content of the wrong length is rejected, as the real storage does.</summary>
internal sealed class FakeFileStorage : IFileStorage
{
    private readonly Dictionary<string, byte[]> _files = [];

    public IReadOnlyDictionary<string, byte[]> Files => _files;

    /// <summary>Gets or sets a value indicating whether the storage rejects every file as not matching its type.</summary>
    public bool RejectSignature { get; set; }

    public async Task<Result> SaveAsync(
        string storageKey, Stream content, string contentType, long sizeBytes, CancellationToken cancellationToken = default)
    {
        using var buffer = new MemoryStream();
        await content.CopyToAsync(buffer, cancellationToken);
        if (RejectSignature || buffer.Length != sizeBytes)
            return Result.Failure(ApplicationErrors.Document.ContentMismatch);

        _files[storageKey] = buffer.ToArray();

        return Result.Success();
    }

    public Task<Stream?> OpenReadAsync(string storageKey, CancellationToken cancellationToken = default) =>
        Task.FromResult<Stream?>(_files.TryGetValue(storageKey, out var bytes) ? new MemoryStream(bytes, writable: false) : null);

    public Task DeleteAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        _files.Remove(storageKey);

        return Task.CompletedTask;
    }

    /// <summary>Stores a file that exists before the test acts.</summary>
    public void Seed(string storageKey, byte[] bytes) => _files[storageKey] = bytes;
}
