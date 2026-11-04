using AutoElite.Application.Common.Abstractions;
using AutoElite.Application.Common.Events;

namespace AutoElite.Application.Tests.Events;

public class DomainEventPublisherTests
{
    private static readonly DateTime _now = new(2026, 3, 1, 8, 0, 0, DateTimeKind.Utc);

    private readonly CallJournal _journal = new();
    private readonly FakeUnitOfWork _unitOfWork = new();
    private readonly ListLogger<DomainEventPublisher> _logger = new();

    private IDomainEventPublisher CreatePublisher(params Type[] handlerTypes)
    {
        var services = new ServiceCollection();
        services.AddSingleton(_journal);
        services.AddSingleton<IUnitOfWork>(_unitOfWork);
        services.AddSingleton<ILogger<DomainEventPublisher>>(_logger);
        services.AddApplication();

        foreach (var handlerType in handlerTypes)
            services.AddScoped(typeof(IDomainEventHandler<TestDomainEvent>), handlerType);

        return services.BuildServiceProvider().GetRequiredService<IDomainEventPublisher>();
    }

    [Fact]
    public async Task PublishAsync_EventWithHandlers_RunsThemInOrderThenSavesOnce()
    {
        var publisher = CreatePublisher(typeof(FirstTestEventHandler), typeof(SecondTestEventHandler));

        await publisher.PublishAsync(new TestDomainEvent("lesson", _now), TestContext.Current.CancellationToken);

        _journal.Entries.Should().Equal("first:lesson", "second:lesson");
        _unitOfWork.SaveCount.Should().Be(1);
    }

    [Fact]
    public async Task PublishAsync_PassesTheCancellationTokenToHandlersAndSave()
    {
        var publisher = CreatePublisher(typeof(FirstTestEventHandler));
        using var cancellation = new CancellationTokenSource();

        await publisher.PublishAsync(new TestDomainEvent("lesson", _now), cancellation.Token);

        _journal.LastToken.Should().Be(cancellation.Token);
        _unitOfWork.LastToken.Should().Be(cancellation.Token);
    }

    [Fact]
    public async Task PublishAsync_EventWithoutHandlers_DoesNotSave()
    {
        var publisher = CreatePublisher(typeof(FirstTestEventHandler));

        await publisher.PublishAsync(new UnhandledDomainEvent(_now), TestContext.Current.CancellationToken);

        _journal.Entries.Should().BeEmpty();
        _unitOfWork.SaveCount.Should().Be(0);
    }

    [Fact]
    public async Task PublishAsync_HandlerThrows_PropagatesAndDoesNotSave()
    {
        var publisher = CreatePublisher(typeof(FirstTestEventHandler), typeof(FailingTestEventHandler));

        var act = () => publisher.PublishAsync(new TestDomainEvent("lesson", _now), TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<InvalidOperationException>();
        _unitOfWork.SaveCount.Should().Be(0);
    }

    [Fact]
    public async Task PublishAsync_EventWithHandlers_LogsTheEventAndHandlerCount()
    {
        var publisher = CreatePublisher(typeof(FirstTestEventHandler), typeof(SecondTestEventHandler));

        await publisher.PublishAsync(new TestDomainEvent("lesson", _now), TestContext.Current.CancellationToken);

        _logger.Entries.Should().ContainSingle()
            .Which.Should().Be(new LogEntry(LogLevel.Information, "Published TestDomainEvent to 2 handlers"));
    }

    [Fact]
    public async Task PublishAsync_NullEvent_Throws()
    {
        var publisher = CreatePublisher();

        var act = () => publisher.PublishAsync(null!, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }
}
