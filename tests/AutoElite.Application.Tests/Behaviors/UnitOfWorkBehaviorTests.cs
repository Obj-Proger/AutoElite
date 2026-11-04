using AutoElite.Application.Common.Abstractions;

namespace AutoElite.Application.Tests.Behaviors;

public class UnitOfWorkBehaviorTests
{
    private static readonly Error _handlerError = Error.Conflict("Test.Failed", "Handler failed.");

    private static Task<Result> HandleCommand(FakeUnitOfWork unitOfWork, Result response, CancellationToken cancellationToken) =>
        new UnitOfWorkBehavior<TestCommand, Result>(unitOfWork)
            .Handle(new TestCommand("name"), () => Task.FromResult(response), cancellationToken);

    [Fact]
    public async Task Handle_SuccessfulCommand_SavesOnceWithTheRequestToken()
    {
        var unitOfWork = new FakeUnitOfWork();
        using var cancellation = new CancellationTokenSource();

        var result = await HandleCommand(unitOfWork, Result.Success(), cancellation.Token);

        result.IsSuccess.Should().BeTrue();
        unitOfWork.SaveCount.Should().Be(1);
        unitOfWork.LastToken.Should().Be(cancellation.Token);
    }

    [Fact]
    public async Task Handle_FailedCommand_ReturnsTheFailureWithoutSaving()
    {
        var unitOfWork = new FakeUnitOfWork();

        var result = await HandleCommand(unitOfWork, Result.Failure(_handlerError), TestContext.Current.CancellationToken);

        result.Error.Should().Be(_handlerError);
        unitOfWork.SaveCount.Should().Be(0);
    }

    [Fact]
    public async Task Handle_SuccessfulCommandWithValue_SavesAndReturnsTheValue()
    {
        var unitOfWork = new FakeUnitOfWork();
        var id = Guid.CreateVersion7();
        var behavior = new UnitOfWorkBehavior<CreateTestCommand, Result<Guid>>(unitOfWork);

        var result = await behavior.Handle(
            new CreateTestCommand("name"), () => Task.FromResult(Result.Success(id)), CancellationToken.None);

        result.Value.Should().Be(id);
        unitOfWork.SaveCount.Should().Be(1);
    }

    [Fact]
    public async Task Handle_Query_DoesNotSave()
    {
        var unitOfWork = new FakeUnitOfWork();
        var behavior = new UnitOfWorkBehavior<TestQuery, Result<int>>(unitOfWork);

        var result = await behavior.Handle(new TestQuery(2), () => Task.FromResult(Result.Success(4)), CancellationToken.None);

        result.Value.Should().Be(4);
        unitOfWork.SaveCount.Should().Be(0);
    }

    [Fact]
    public async Task Handle_ConcurrencyConflictOnSave_ReturnsConflictError()
    {
        var unitOfWork = new FakeUnitOfWork { ExceptionToThrow = new ConcurrencyConflictException() };
        var behavior = new UnitOfWorkBehavior<CreateTestCommand, Result<Guid>>(unitOfWork);

        var result = await behavior.Handle(
            new CreateTestCommand("name"), () => Task.FromResult(Result.Success(Guid.CreateVersion7())), CancellationToken.None);

        result.Error.Should().Be(ApplicationErrors.Concurrency.Conflict);
    }

    [Fact]
    public async Task Handle_OtherExceptionOnSave_Propagates()
    {
        var unitOfWork = new FakeUnitOfWork { ExceptionToThrow = new InvalidOperationException("Database is down.") };

        var act = () => HandleCommand(unitOfWork, Result.Success(), TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }
}
