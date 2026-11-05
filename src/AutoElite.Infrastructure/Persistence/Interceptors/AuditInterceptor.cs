using Microsoft.EntityFrameworkCore.Diagnostics;

namespace AutoElite.Infrastructure.Persistence.Interceptors;

/// <summary>
/// Fills in the audit properties of <see cref="IAuditable"/> entities when they are saved: the time and the user of
/// creation for new entities, and of the last change for changed ones. A change made by the system, such as a
/// scheduled job, has no user.
/// </summary>
/// <remarks>
/// The domain gives these properties private setters and never changes them; EF Core writes them through the change
/// tracker.
/// </remarks>
internal sealed class AuditInterceptor(ICurrentUser currentUser, TimeProvider timeProvider) : SaveChangesInterceptor
{
    /// <inheritdoc />
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        Stamp(eventData.Context);

        return base.SavingChanges(eventData, result);
    }

    /// <inheritdoc />
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        Stamp(eventData.Context);

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void Stamp(DbContext? context)
    {
        if (context is null)
            return;

        var utcNow = timeProvider.GetUtcNow().UtcDateTime;
        var userId = currentUser.UserId;

        foreach (var entry in context.ChangeTracker.Entries<IAuditable>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Property(nameof(IAuditable.CreatedAtUtc)).CurrentValue = utcNow;
                    entry.Property(nameof(IAuditable.CreatedByUserId)).CurrentValue = userId;
                    break;
                case EntityState.Modified:
                    entry.Property(nameof(IAuditable.ModifiedAtUtc)).CurrentValue = utcNow;
                    entry.Property(nameof(IAuditable.ModifiedByUserId)).CurrentValue = userId;
                    break;
            }
        }
    }
}
