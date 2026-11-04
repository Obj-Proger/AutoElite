namespace AutoElite.Application.Common.Events;

/// <summary>
/// Reacts to a domain event after the transaction that raised it has been committed, for example by creating
/// notifications. An event type may have any number of handlers.
/// </summary>
/// <remarks>
/// Events are stored in the transactional outbox and delivered at least once. The database changes of all handlers of
/// an event are committed together with the processed mark of its outbox message (see <see cref="IDomainEventPublisher"/>),
/// so they are applied exactly once; if any handler throws, nothing is committed and the event is delivered again to
/// all of its handlers later. A handler therefore changes only the database and never calls outside services
/// directly; work such as an email goes through a queue that joins the unit of work (<see cref="IEmailQueue"/>).
/// Handlers still tolerate a state that has moved on since the event, for example a record that no longer exists.
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
