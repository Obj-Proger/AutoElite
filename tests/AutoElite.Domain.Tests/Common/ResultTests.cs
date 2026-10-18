namespace AutoElite.Domain.Tests.Common;

public class ResultTests
{
    private static readonly Error _error = Error.Validation("Test.Invalid", "Something is invalid.");

    [Fact]
    public void Success_ReturnsResultWithNoError()
    {
        var result = Result.Success();

        result.IsSuccess.Should().BeTrue();
        result.IsFailure.Should().BeFalse();
        result.Error.Should().Be(Error.None);
    }

    [Fact]
    public void Failure_ReturnsResultWithGivenError()
    {
        var result = Result.Failure(_error);

        result.IsSuccess.Should().BeFalse();
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(_error);
    }

    [Fact]
    public void GenericSuccess_ExposesValue()
    {
        var result = Result.Success(42);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(42);
    }

    [Fact]
    public void GenericFailure_CarriesGivenError()
    {
        var result = Result.Failure<int>(_error);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(_error);
    }

    [Fact]
    public void GenericFailure_AccessingValue_Throws()
    {
        var result = Result.Failure<int>(_error);
        var action = () => result.Value;

        action.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void ImplicitConversion_FromValue_CreatesSuccessResult()
    {
        Result<string> result = "hello";

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be("hello");
    }

    [Fact]
    public void Constructor_SuccessfulResultWithError_Throws()
    {
        var action = () => new Result<int>(1, true, _error);

        action.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Constructor_FailedResultWithoutError_Throws()
    {
        var action = () => new Result<int>(default, false, Error.None);

        action.Should().Throw<InvalidOperationException>();
    }
}
