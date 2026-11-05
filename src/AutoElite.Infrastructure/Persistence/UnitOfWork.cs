using Npgsql;

namespace AutoElite.Infrastructure.Persistence;

/// <summary>Saves the tracked changes of a request in one transaction.</summary>
/// <remarks>
/// A stale row version means another request changed the record after it was loaded. A violated unique index or
/// exclusion constraint means another request saved a conflicting record at the same time, after the application had
/// checked that there was none, for example a branch with the same name. Both are reported as a concurrency conflict,
/// so the client reloads and retries, and the retry gets the proper error from the application's own check.
/// <para>
/// A repository that must lock rows until the request ends, such as the count of administrators, opens a transaction
/// first (<see cref="Transactions.BeginIfNoneAsync"/>); the save then commits it. If the request fails, nothing is
/// saved and the transaction is rolled back when the context is disposed.
/// </para>
/// </remarks>
internal sealed class UnitOfWork(AutoEliteDbContext dbContext) : IUnitOfWork
{
    /// <inheritdoc />
    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);

            if (dbContext.Database.CurrentTransaction is { } transaction)
                await transaction.CommitAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException exception)
        {
            throw new ConcurrencyConflictException("The record was changed by another request after it was loaded.", exception);
        }
        catch (DbUpdateException exception) when (ViolatesUniqueness(exception))
        {
            throw new ConcurrencyConflictException("Another request saved a conflicting record at the same time.", exception);
        }
    }

    private static bool ViolatesUniqueness(DbUpdateException exception) =>
        exception.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.UniqueViolation or PostgresErrorCodes.ExclusionViolation
        };
}
