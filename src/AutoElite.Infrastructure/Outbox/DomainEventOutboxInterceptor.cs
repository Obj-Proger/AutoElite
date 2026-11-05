using Microsoft.EntityFrameworkCore.Diagnostics;

namespace AutoElite.Infrastructure.Outbox;

/// <summary>
/// Moves the domain events of the tracked entities into the outbox as part of the same save, including the events of
/// entities being deleted, so that an event is stored if and only if the change that raised it is.
/// </summary>
/// <remarks>
/// The events are cleared from the entities as they are moved, so that saving again does not store them twice. Every
/// event is stored, even one without handlers: the publisher then only marks it delivered.
/// </remarks>
internal sealed class DomainEventOutboxInterceptor : SaveChangesInterceptor
{
    /// <summary>Gets the interceptor; it keeps no state, so one serves every context.</summary>
    public static DomainEventOutboxInterceptor Instance { get; } = new();

    private DomainEventOutboxInterceptor()
    {
    }

    /// <inheritdoc />
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        MoveToOutbox(eventData.Context);

        return base.SavingChanges(eventData, result);
    }

    /// <inheritdoc />
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        MoveToOutbox(eventData.Context);

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private static void MoveToOutbox(DbContext? context)
    {
        if (context is null)
            return;

        var entities = context.ChangeTracker.Entries<IHasDomainEvents>()
            .Select(entry => entry.Entity)
            .Where(entity => entity.DomainEvents.Count > 0)
            .ToList();

        foreach (var entity in entities)
        {
            foreach (var domainEvent in entity.DomainEvents)
                context.Set<OutboxMessage>().Add(OutboxMessage.Create(domainEvent));

            entity.ClearDomainEvents();
        }
    }
}
