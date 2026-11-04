namespace AutoElite.Application.Common.Events;

/// <summary>
/// Delivers one domain event from the outbox to its handlers. Called by the infrastructure's outbox processor,
/// one event per dependency injection scope.
/// </summary>
public interface IDomainEventPublisher
{
    /// <summary>
    /// Runs every handler of the event's runtime type, then saves their changes in one transaction.
    /// If a handler throws, nothing is saved and the exception propagates, so the outbox delivers the event again.
    /// </summary>
    /// <param name="domainEvent">The event read from the outbox.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    Task PublishAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default);
}
