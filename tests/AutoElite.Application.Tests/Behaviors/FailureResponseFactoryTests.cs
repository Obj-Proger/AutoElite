namespace AutoElite.Application.Tests.Behaviors;

public class FailureResponseFactoryTests
{
    private static readonly Error _error = Error.Failure("Test.Failed", "Something failed.");

    [Fact]
    public void Create_ForResult_ReturnsFailedResultWithGivenError()
    {
        var response = FailureResponseFactory<Result>.Create(_error);

        response.IsFailure.Should().BeTrue();
        response.Error.Should().Be(_error);
    }

    [Fact]
    public void Create_ForGenericResult_ReturnsFailedResultOfRequestedType()
    {
        var response = FailureResponseFactory<Result<int>>.Create(_error);

        response.IsFailure.Should().BeTrue();
        response.Error.Should().Be(_error);
    }
}
