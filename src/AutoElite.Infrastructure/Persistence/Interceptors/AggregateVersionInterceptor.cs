using AutoElite.Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Metadata;

namespace AutoElite.Infrastructure.Persistence.Interceptors;

/// <summary>
/// Makes a change to a child entity, such as a note added to a lead, a change of its aggregate root, so that the
/// root's row version is checked and advanced: two requests that change the same aggregate through different children
/// then conflict instead of both succeeding on a stale copy of the root.
/// </summary>
/// <remarks>
/// A child is an entity whose foreign key points to an entity with a row version and is the target of a collection
/// navigation of that entity; a foreign key between aggregates has no navigation, so it does not count. An unchanged
/// root is marked as changed through its time of change when it is audited, which the audit then fills in.
/// </remarks>
internal sealed class AggregateVersionInterceptor : SaveChangesInterceptor
{
    /// <summary>Gets the interceptor; it keeps no state, so one serves every context.</summary>
    public static AggregateVersionInterceptor Instance { get; } = new();

    private AggregateVersionInterceptor()
    {
    }

    /// <inheritdoc />
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        TouchRoots(eventData.Context);

        return base.SavingChanges(eventData, result);
    }

    /// <inheritdoc />
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        TouchRoots(eventData.Context);

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private static void TouchRoots(DbContext? context)
    {
        if (context is null)
            return;

        var entries = context.ChangeTracker.Entries().ToList();
        var changedChildren = entries.Where(entry => entry.State is EntityState.Added or EntityState.Modified or EntityState.Deleted
            && OwnershipOf(entry.Metadata) is not null);

        foreach (var child in changedChildren.ToList())
        {
            var ownership = OwnershipOf(child.Metadata)!;
            var rootId = ownership.Properties.Select(property => KeyValue(child, property)).ToArray();
            var root = entries.FirstOrDefault(entry => entry.Metadata == ownership.PrincipalEntityType
                && ownership.PrincipalKey.Properties.Select(property => entry.Property(property.Name).CurrentValue)
                    .SequenceEqual(rootId));

            if (root is not { State: EntityState.Unchanged })
                continue;

            if (root.Entity is IAuditable)
                root.Property(nameof(IAuditable.ModifiedAtUtc)).IsModified = true;
            else
                root.State = EntityState.Modified;
        }
    }

    /// <summary>Finds the foreign key that ties a child to its aggregate root, if the entity is a child.</summary>
    private static IForeignKey? OwnershipOf(IEntityType entityType) =>
        entityType.GetForeignKeys().FirstOrDefault(foreignKey => foreignKey.PrincipalToDependent is { IsCollection: true }
            && foreignKey.PrincipalEntityType.FindProperty(EntityTypeBuilderExtensions.RowVersion) is not null);

    /// <summary>Reads the foreign key value the child had when loaded, so that a deleted child still finds its root.</summary>
    private static object? KeyValue(EntityEntry child, IProperty property) =>
        child.State == EntityState.Added ? child.Property(property.Name).CurrentValue : child.Property(property.Name).OriginalValue;
}
