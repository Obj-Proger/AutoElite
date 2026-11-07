using AutoElite.Application.Documents;
using AutoElite.Infrastructure.Persistence;
using AutoElite.Infrastructure.Storage;
using Hangfire;
using Microsoft.Extensions.Options;

namespace AutoElite.Infrastructure.BackgroundJobs;

/// <summary>
/// The daily removal of stored files that no document names (D-062): files whose document was never committed, or
/// whose removal after the document's deletion failed, and files left half-received.
/// </summary>
/// <remarks>
/// Only files older than <see cref="FileStorageOptions.OrphanAge"/> are looked at, since a younger one may belong to
/// a document whose save has not committed yet. The keys are checked against the documents in batches.
/// </remarks>
internal sealed class FileCleanupJob(
    AutoEliteDbContext dbContext,
    IStoredFiles storedFiles,
    IFileStorage fileStorage,
    TimeProvider timeProvider,
    IOptions<FileStorageOptions> options)
{
    private const int BatchSize = 500;

    /// <summary>Removes the files no document names.</summary>
    /// <param name="cancellationToken">The token Hangfire cancels when the server stops.</param>
    /// <returns>The files removed, not counting half-received ones.</returns>
    [AutomaticRetry(Attempts = 0)]
    [DisableConcurrentExecution(60 * 60)]
    public async Task<int> RemoveOrphansAsync(CancellationToken cancellationToken)
    {
        var storedBeforeUtc = timeProvider.GetUtcNow().UtcDateTime - options.Value.OrphanAge;
        await storedFiles.RemoveAbandonedIncomingAsync(storedBeforeUtc, cancellationToken);

        var removed = 0;
        var batch = new List<string>(BatchSize);
        await foreach (var key in storedFiles.ListKeysAsync(storedBeforeUtc, cancellationToken))
        {
            batch.Add(key);
            if (batch.Count == BatchSize)
            {
                removed += await RemoveUnnamedAsync(batch, cancellationToken);
                batch.Clear();
            }
        }

        return removed + await RemoveUnnamedAsync(batch, cancellationToken);
    }

    private async Task<int> RemoveUnnamedAsync(List<string> keys, CancellationToken cancellationToken)
    {
        if (keys.Count == 0)
            return 0;

        var named = (await dbContext.Set<Document>().AsNoTracking()
                .Where(document => keys.Contains(document.StorageKey))
                .Select(document => document.StorageKey)
                .ToListAsync(cancellationToken))
            .ToHashSet(StringComparer.Ordinal);
        var removed = 0;
        foreach (var key in keys.Where(candidate => !named.Contains(candidate)))
        {
            await fileStorage.DeleteAsync(key, cancellationToken);
            removed++;
        }

        return removed;
    }
}
