using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using AutoElite.Application.Common;
using AutoElite.Application.Documents;
using Microsoft.Extensions.Options;

namespace AutoElite.Infrastructure.Storage;

/// <summary>Lists the kept files, for the clean-up of files that no document names.</summary>
internal interface IStoredFiles
{
    /// <summary>Lists the keys of the files kept before the given time.</summary>
    /// <param name="storedBeforeUtc">The UTC time the files were kept before.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    IAsyncEnumerable<string> ListKeysAsync(DateTime storedBeforeUtc, CancellationToken cancellationToken);

    /// <summary>Removes the files left half-received by a host that stopped while receiving them.</summary>
    /// <param name="storedBeforeUtc">The UTC time the files were left before.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    Task RemoveAbandonedIncomingAsync(DateTime storedBeforeUtc, CancellationToken cancellationToken);
}

/// <summary>The rules of storage keys and local files, shared by the providers.</summary>
internal static partial class LocalFiles
{
    /// <summary>The folder of the files of documents under the root, which is the first part of their keys.</summary>
    public const string DocumentsFolder = "documents";

    /// <summary>The folder of files being received, under the root.</summary>
    public const string IncomingFolder = ".incoming";

    /// <summary>
    /// Checks that a key is one the application makes, <c>documents/</c> and 32 hexadecimal digits, so that a key can
    /// never point outside the storage.
    /// </summary>
    /// <param name="storageKey">The key.</param>
    /// <exception cref="ArgumentException">The key is not one the application makes.</exception>
    public static void EnsureValidKey(string storageKey)
    {
        if (!KeyPattern().IsMatch(storageKey))
            throw new ArgumentException($"\"{storageKey}\" is not a storage key of a document.", nameof(storageKey));
    }

    /// <summary>
    /// Creates a new file. On Unix only its owner may read and write it and nobody may execute it; on Windows, where
    /// the setting does not exist and setting it at all throws, the file takes the permissions of its folder.
    /// </summary>
    /// <param name="path">The path of the file, which must not exist.</param>
    public static FileStream CreateNew(string path)
    {
        var options = new FileStreamOptions
        {
            Mode = FileMode.CreateNew,
            Access = FileAccess.Write,
            Share = FileShare.None,
            Options = FileOptions.Asynchronous
        };
        if (!OperatingSystem.IsWindows())
            options.UnixCreateMode = UnixFileMode.UserRead | UnixFileMode.UserWrite;

        return new FileStream(path, options);
    }

    [GeneratedRegex("^documents/[0-9a-f]{32}$", RegexOptions.CultureInvariant)]
    private static partial Regex KeyPattern();
}

/// <summary>
/// Keeps the files of documents in a folder on the server's disk, under their keys: <c>documents/{32 hex digits}</c>.
/// A file is received into <c>.incoming</c> on the same disk and moved to its place only once it is checked, so that
/// a half-received or rejected file is never kept.
/// </summary>
internal sealed class LocalFileStorage(IOptions<FileStorageOptions> options) : IFileStorage, IStoredFiles
{
    private string Root => Path.GetFullPath(options.Value.RootPath);

    /// <inheritdoc />
    public async Task<Result> SaveAsync(
        string storageKey, Stream content, string contentType, long sizeBytes, CancellationToken cancellationToken = default)
    {
        LocalFiles.EnsureValidKey(storageKey);
        await using var staged = await StagedFile.ReceiveAsync(
            Path.Combine(Root, LocalFiles.IncomingFolder), content, contentType, sizeBytes, cancellationToken);
        if (!staged.IsValid)
            return Result.Failure(ApplicationErrors.Document.ContentMismatch);

        staged.MoveTo(PathOf(storageKey));

        return Result.Success();
    }

    /// <inheritdoc />
    public Task<Stream?> OpenReadAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        LocalFiles.EnsureValidKey(storageKey);
        try
        {
            Stream stream = new FileStream(
                PathOf(storageKey), FileMode.Open, FileAccess.Read, FileShare.Read, 81920,
                FileOptions.Asynchronous | FileOptions.SequentialScan);

            return Task.FromResult<Stream?>(stream);
        }
        catch (Exception exception) when (exception is FileNotFoundException or DirectoryNotFoundException)
        {
            return Task.FromResult<Stream?>(null);
        }
    }

    /// <inheritdoc />
    public Task DeleteAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        LocalFiles.EnsureValidKey(storageKey);
        File.Delete(PathOf(storageKey));

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<string> ListKeysAsync(
        DateTime storedBeforeUtc, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        // Listing a folder has no asynchronous form; the interface is asynchronous for object storages.
        await Task.CompletedTask;
        var folder = Path.Combine(Root, LocalFiles.DocumentsFolder);
        if (!Directory.Exists(folder))
            yield break;

        foreach (var file in new DirectoryInfo(folder).EnumerateFiles())
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (file.LastWriteTimeUtc < storedBeforeUtc)
                yield return $"{LocalFiles.DocumentsFolder}/{file.Name}";
        }
    }

    /// <inheritdoc />
    public Task RemoveAbandonedIncomingAsync(DateTime storedBeforeUtc, CancellationToken cancellationToken)
    {
        var folder = Path.Combine(Root, LocalFiles.IncomingFolder);
        if (Directory.Exists(folder))
        {
            foreach (var file in new DirectoryInfo(folder).EnumerateFiles("*.part"))
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (file.LastWriteTimeUtc < storedBeforeUtc)
                    file.Delete();
            }
        }

        return Task.CompletedTask;
    }

    private string PathOf(string storageKey) =>
        Path.Combine(Root, LocalFiles.DocumentsFolder, storageKey[(LocalFiles.DocumentsFolder.Length + 1)..]);
}
