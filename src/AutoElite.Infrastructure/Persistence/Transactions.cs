using System.Diagnostics.CodeAnalysis;

namespace AutoElite.Infrastructure.Persistence;

/// <summary>The transaction of a request, for reads that must hold their locks until the changes are saved.</summary>
internal static class Transactions
{
    /// <summary>
    /// Opens a transaction unless one is open, so that the rows a following query locks stay locked until
    /// <see cref="UnitOfWork"/> commits.
    /// </summary>
    /// <param name="dbContext">The context of the request.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    [SuppressMessage("Reliability", "CA2000", Justification = "The context owns its current transaction and disposes it.")]
    public static async Task BeginIfNoneAsync(this AutoEliteDbContext dbContext, CancellationToken cancellationToken)
    {
        if (dbContext.Database.CurrentTransaction is null)
            await dbContext.Database.BeginTransactionAsync(cancellationToken);
    }
}
