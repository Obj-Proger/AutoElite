using AutoElite.Application.Common.Events;

namespace AutoElite.Application.Tests.TestDoubles;

internal sealed record TestDomainEvent(string Name, DateTime OccurredOnUtc) : IDomainEvent;

internal sealed record UnhandledDomainEvent(DateTime OccurredOnUtc) : IDomainEvent;

internal sealed class FirstTestEventHandler(CallJournal journal) : IDomainEventHandler<TestDomainEvent>
{
    public Task Handle(TestDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        journal.Add($"first:{domainEvent.Name}");
        journal.LastToken = cancellationToken;

        return Task.CompletedTask;
    }
}

internal sealed class SecondTestEventHandler(CallJournal journal) : IDomainEventHandler<TestDomainEvent>
{
    public Task Handle(TestDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        journal.Add($"second:{domainEvent.Name}");

        return Task.CompletedTask;
    }
}

internal sealed class FailingTestEventHandler : IDomainEventHandler<TestDomainEvent>
{
    public Task Handle(TestDomainEvent domainEvent, CancellationToken cancellationToken) =>
        throw new InvalidOperationException("Handler failed.");
}
