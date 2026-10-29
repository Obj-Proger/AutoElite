namespace AutoElite.Application.Tests.Messaging;

public class DispatcherTests
{
    private static readonly Error _error = Error.Failure("Test.Failed", "Something failed.");

    private static (Dispatcher Dispatcher, CallJournal Journal) CreateDispatcher(Action<IServiceCollection>? configure = null)
    {
        var journal = new CallJournal();
        var services = new ServiceCollection();
        services.AddSingleton(journal);
        services.AddScoped<IRequestHandler<TestCommand, Result>, TestCommandHandler>();
        services.AddScoped<IRequestHandler<TestQuery, Result<int>>, TestQueryHandler>();
        configure?.Invoke(services);

        return (new Dispatcher(services.BuildServiceProvider()), journal);
    }

    [Fact]
    public async Task Send_Command_InvokesHandlerAndReturnsItsResult()
    {
        var (dispatcher, journal) = CreateDispatcher();

        var result = await dispatcher.Send(new TestCommand("name"), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        journal.Entries.Should().Equal("handler");
    }

    [Fact]
    public async Task Send_Query_ReturnsHandlerValue()
    {
        var (dispatcher, _) = CreateDispatcher();

        var result = await dispatcher.Send(new TestQuery(21), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(42);
    }

    [Fact]
    public async Task Send_SameRequestTypeTwice_InvokesHandlerEachTime()
    {
        var (dispatcher, journal) = CreateDispatcher();

        await dispatcher.Send(new TestCommand("first"), TestContext.Current.CancellationToken);
        await dispatcher.Send(new TestCommand("second"), TestContext.Current.CancellationToken);

        journal.Entries.Should().Equal("handler", "handler");
    }

    [Fact]
    public async Task Send_WithBehaviors_RunsThemAroundHandlerInRegistrationOrder()
    {
        var (dispatcher, journal) = CreateDispatcher(services =>
        {
            services.AddScoped(typeof(IPipelineBehavior<,>), typeof(FirstBehavior<,>));
            services.AddScoped(typeof(IPipelineBehavior<,>), typeof(SecondBehavior<,>));
        });

        await dispatcher.Send(new TestCommand("name"), TestContext.Current.CancellationToken);

        journal.Entries.Should().Equal("first:before", "second:before", "handler", "second:after", "first:after");
    }

    [Fact]
    public async Task Send_BehaviorShortCircuits_ReturnsItsResultWithoutInvokingHandler()
    {
        var (dispatcher, journal) = CreateDispatcher(services =>
            services.AddScoped<IPipelineBehavior<TestCommand, Result>>(_ => new ShortCircuitBehavior(_error)));

        var result = await dispatcher.Send(new TestCommand("name"), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(_error);
        journal.Entries.Should().BeEmpty();
    }

    [Fact]
    public async Task Send_PassesCancellationTokenToHandler()
    {
        var (dispatcher, journal) = CreateDispatcher();
        using var tokenSource = new CancellationTokenSource();

        await dispatcher.Send(new TestCommand("name"), tokenSource.Token);

        journal.LastToken.Should().Be(tokenSource.Token);
    }

    [Fact]
    public async Task Send_NullRequest_ThrowsArgumentNullException()
    {
        var (dispatcher, _) = CreateDispatcher();
        var action = () => dispatcher.Send<Result>(null!, TestContext.Current.CancellationToken);

        await action.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task Send_RequestWithoutRegisteredHandler_ThrowsInvalidOperationException()
    {
        var (dispatcher, _) = CreateDispatcher();
        var action = () => dispatcher.Send(new UnhandledCommand(), TestContext.Current.CancellationToken);

        await action.Should().ThrowAsync<InvalidOperationException>();
    }
}
