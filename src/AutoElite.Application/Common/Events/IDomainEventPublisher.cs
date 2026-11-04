namespace AutoElite.Application.Common.Events;

/// <summary>
/// Delivers one domain event from the outbox to its handlers. Called by the infrastructure's outbox processor,
/// one event per dependency injection scope.
/// </summary>
/// <remarks>
/// Before calling, the processor marks the outbox message as processed in the unit of work of the same scope. The
/// publisher always saves, so the changes of the handlers and that mark are committed in one transaction: delivery
/// is at least once, but the effects of an event are applied exactly once (the transactional inbox on the database
/// of the outbox). If a handler throws, nothing is saved, the mark included, and the event is delivered again later.
/// </remarks>
public interface IDomainEventPublisher
{
    /// <summary>
    /// Runs every handler of the event's runtime type, then saves their changes together with the processed mark of
    /// the outbox message in one transaction, even when the event has no handlers.
    /// If a handler throws, nothing is saved and the exception propagates, so the outbox delivers the event again.
    /// </summary>
    /// <param name="domainEvent">The event read from the outbox.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    Task PublishAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default);
}
