using System.Collections.Concurrent;
using AutoElite.Application.Common.Behaviors;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AutoElite.Application.Common.Events;

/// <summary>Default <see cref="IDomainEventPublisher"/>: finds the handlers of an event through the service provider.</summary>
internal sealed class DomainEventPublisher(
    IServiceProvider serviceProvider,
    IUnitOfWork unitOfWork,
    ILogger<DomainEventPublisher> logger) : IDomainEventPublisher
{
    private static readonly ConcurrentDictionary<Type, DomainEventHandlerWrapperBase> _wrappers = new();

    /// <inheritdoc />
    public async Task PublishAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);

        var eventType = domainEvent.GetType();
        var wrapper = _wrappers.GetOrAdd(eventType, type =>
            (DomainEventHandlerWrapperBase)Activator.CreateInstance(typeof(DomainEventHandlerWrapper<>).MakeGenericType(type))!);

        var handledCount = await wrapper.Handle(domainEvent, serviceProvider, cancellationToken);

        // Saved even without handlers, because the processed mark of the outbox message rides on the same transaction.
        await unitOfWork.SaveChangesAsync(cancellationToken);
        Log.PublishedDomainEvent(logger, eventType.Name, handledCount);
    }
}

/// <summary>Non-generic view of <see cref="DomainEventHandlerWrapper{TEvent}"/> for an event type known only at runtime.</summary>
internal abstract class DomainEventHandlerWrapperBase
{
    /// <summary>Runs every handler of the event and returns how many there were.</summary>
    public abstract Task<int> Handle(IDomainEvent domainEvent, IServiceProvider serviceProvider, CancellationToken cancellationToken);
}

/// <summary>Resolves and runs the handlers of one event type, in registration order.</summary>
internal sealed class DomainEventHandlerWrapper<TEvent> : DomainEventHandlerWrapperBase
    where TEvent : IDomainEvent
{
    public override async Task<int> Handle(IDomainEvent domainEvent, IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        var typedEvent = (TEvent)domainEvent;
        var handledCount = 0;

        foreach (var handler in serviceProvider.GetServices<IDomainEventHandler<TEvent>>())
        {
            await handler.Handle(typedEvent, cancellationToken);
            handledCount++;
        }

        return handledCount;
    }
}
