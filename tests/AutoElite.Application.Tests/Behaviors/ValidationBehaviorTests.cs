namespace AutoElite.Application.Tests.Behaviors;

public class ValidationBehaviorTests
{
    [Fact]
    public async Task Handle_NoValidators_CallsNext()
    {
        var behavior = new ValidationBehavior<TestCommand, Result>([]);
        var nextCalled = false;

        var result = await behavior.Handle(new TestCommand(string.Empty), Next, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        nextCalled.Should().BeTrue();

        Task<Result> Next()
        {
            nextCalled = true;

            return Task.FromResult(Result.Success());
        }
    }

    [Fact]
    public async Task Handle_ValidRequest_CallsNext()
    {
        var behavior = new ValidationBehavior<TestCommand, Result>([new TestCommandValidator(), new TestCommandNumberValidator()]);
        var nextCalled = false;

        var result = await behavior.Handle(new TestCommand("name", 1), Next, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        nextCalled.Should().BeTrue();

        Task<Result> Next()
        {
            nextCalled = true;

            return Task.FromResult(Result.Success());
        }
    }

    [Fact]
    public async Task Handle_InvalidRequest_ReturnsValidationErrorWithFieldErrorsAndSkipsNext()
    {
        var behavior = new ValidationBehavior<TestCommand, Result>([new TestCommandValidator()]);
        var nextCalled = false;

        var result = await behavior.Handle(new TestCommand(string.Empty), Next, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Validation.Failed");
        result.Error.Type.Should().Be(ErrorType.Validation);
        result.Error.FieldErrors.Should().Equal(new FieldError("Name", "Test.NameEmpty", "Name is required."));
        nextCalled.Should().BeFalse();

        Task<Result> Next()
        {
            nextCalled = true;

            return Task.FromResult(Result.Success());
        }
    }

    [Fact]
    public async Task Handle_FailuresInSeveralValidators_ReportsAllOfThemInValidatorOrder()
    {
        var behavior = new ValidationBehavior<TestCommand, Result>([new TestCommandValidator(), new TestCommandNumberValidator()]);

        var result = await behavior.Handle(new TestCommand(string.Empty, 0), () => Task.FromResult(Result.Success()), CancellationToken.None);

        result.Error.FieldErrors.Should().Equal(
            new FieldError("Name", "Test.NameEmpty", "Name is required."),
            new FieldError("Number", "Test.NumberNotPositive", "Number must be positive."));
    }

    [Fact]
    public async Task Handle_SeveralFailuresInOneValidator_ReportsEachOfThem()
    {
        var behavior = new ValidationBehavior<TestCommand, Result>([new TestCommandValidator()]);

        var result = await behavior.Handle(new TestCommand("far too long name"), () => Task.FromResult(Result.Success()), CancellationToken.None);

        result.Error.FieldErrors.Should().Equal(new FieldError("Name", "Test.NameTooLong", "Name is too long."));
    }

    [Fact]
    public async Task Handle_GenericResponse_ReturnsFailedResultOfRequestedType()
    {
        var behavior = new ValidationBehavior<TestQuery, Result<int>>([new TestQueryValidator()]);

        var result = await behavior.Handle(new TestQuery(0), () => Task.FromResult<Result<int>>(1), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.FieldErrors.Should().Equal(new FieldError("Value", "Test.ValueNotPositive", "Value must be positive."));
    }

    [Fact]
    public async Task Handle_RuleWithoutErrorCode_UsesFallbackCode()
    {
        var behavior = new ValidationBehavior<TestCommand, Result>([new TestCommandWithoutCodeValidator()]);

        var result = await behavior.Handle(new TestCommand("name"), () => Task.FromResult(Result.Success()), CancellationToken.None);

        var fieldError = result.Error.FieldErrors.Should().ContainSingle().Which;
        fieldError.Code.Should().Be(ApplicationErrors.InvalidFieldCode);
        fieldError.Message.Should().Be("Something is wrong.");
    }
}
