namespace AutoElite.Application.Tests.Behaviors;

public class LoggingBehaviorTests
{
    private static readonly Error _error = Error.Failure("Test.Failed", "Something failed.");

    [Fact]
    public async Task Handle_SuccessfulRequest_LogsStartAndCompletion()
    {
        var logger = new ListLogger<LoggingBehavior<TestCommand, Result>>();
        var behavior = new LoggingBehavior<TestCommand, Result>(logger);

        await behavior.Handle(new TestCommand("name"), () => Task.FromResult(Result.Success()), CancellationToken.None);

        logger.Entries.Should().Equal(
            new LogEntry(LogLevel.Information, "Handling TestCommand"),
            new LogEntry(LogLevel.Information, "Handled TestCommand"));
    }

    [Fact]
    public async Task Handle_FailedRequest_LogsWarningWithErrorCodeAndMessage()
    {
        var logger = new ListLogger<LoggingBehavior<TestCommand, Result>>();
        var behavior = new LoggingBehavior<TestCommand, Result>(logger);

        await behavior.Handle(new TestCommand("name"), () => Task.FromResult(Result.Failure(_error)), CancellationToken.None);

        logger.Entries.Should().Equal(
            new LogEntry(LogLevel.Information, "Handling TestCommand"),
            new LogEntry(LogLevel.Warning, "TestCommand failed: [Test.Failed] Something failed."));
    }

    [Fact]
    public async Task Handle_FailedGenericRequest_LogsWarning()
    {
        var logger = new ListLogger<LoggingBehavior<TestQuery, Result<int>>>();
        var behavior = new LoggingBehavior<TestQuery, Result<int>>(logger);

        await behavior.Handle(new TestQuery(1), () => Task.FromResult(Result.Failure<int>(_error)), CancellationToken.None);

        logger.Entries.Should().Contain(new LogEntry(LogLevel.Warning, "TestQuery failed: [Test.Failed] Something failed."));
    }

    [Fact]
    public async Task Handle_ReturnsTheResponseOfTheNextStep()
    {
        var logger = new ListLogger<LoggingBehavior<TestCommand, Result>>();
        var behavior = new LoggingBehavior<TestCommand, Result>(logger);
        var expected = Result.Success();

        var response = await behavior.Handle(new TestCommand("name"), () => Task.FromResult(expected), CancellationToken.None);

        response.Should().BeSameAs(expected);
    }
}
