namespace AutoElite.Application.Common.Events;

/// <summary>
/// Reacts to a domain event after the transaction that raised it has been committed, for example by creating
/// notifications. An event type may have any number of handlers.
/// </summary>
/// <remarks>
/// Events are stored in the transactional outbox and delivered at least once: if any handler throws, the event is
/// delivered again to all of its handlers later. Handlers therefore must not have side effects outside the database
/// that cannot be repeated; such work (an email, a push message) is scheduled as a separate job instead.
/// </remarks>
/// <typeparam name="TEvent">The type of the domain event.</typeparam>
public interface IDomainEventHandler<TEvent>
    where TEvent : IDomainEvent
{
    /// <summary>Handles the event.</summary>
    /// <param name="domainEvent">The event to handle.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    Task Handle(TEvent domainEvent, CancellationToken cancellationToken);
}
