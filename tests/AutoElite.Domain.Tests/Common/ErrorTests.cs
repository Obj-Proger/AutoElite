namespace AutoElite.Domain.Tests.Common;

public class ErrorTests
{
    [Fact]
    public void Failure_SetsFailureType()
    {
        var error = Error.Failure("Test.Code", "Test message.");

        error.Code.Should().Be("Test.Code");
        error.Message.Should().Be("Test message.");
        error.Type.Should().Be(ErrorType.Failure);
    }

    [Fact]
    public void Validation_SetsValidationType()
    {
        var error = Error.Validation("Test.Code", "Test message.");

        error.Code.Should().Be("Test.Code");
        error.Message.Should().Be("Test message.");
        error.Type.Should().Be(ErrorType.Validation);
    }

    [Fact]
    public void NotFound_SetsNotFoundType()
    {
        var error = Error.NotFound("Test.Code", "Test message.");

        error.Code.Should().Be("Test.Code");
        error.Message.Should().Be("Test message.");
        error.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public void Conflict_SetsConflictType()
    {
        var error = Error.Conflict("Test.Code", "Test message.");

        error.Code.Should().Be("Test.Code");
        error.Message.Should().Be("Test message.");
        error.Type.Should().Be(ErrorType.Conflict);
    }

    [Fact]
    public void Unauthorized_SetsUnauthorizedType()
    {
        var error = Error.Unauthorized("Test.Code", "Test message.");

        error.Code.Should().Be("Test.Code");
        error.Message.Should().Be("Test message.");
        error.Type.Should().Be(ErrorType.Unauthorized);
    }

    [Fact]
    public void Forbidden_SetsForbiddenType()
    {
        var error = Error.Forbidden("Test.Code", "Test message.");

        error.Code.Should().Be("Test.Code");
        error.Message.Should().Be("Test message.");
        error.Type.Should().Be(ErrorType.Forbidden);
    }

    [Fact]
    public void None_HasEmptyCodeAndMessage()
    {
        var none = Error.None;

        none.Code.Should().Be(string.Empty);
        none.Message.Should().Be(string.Empty);
    }

    [Fact]
    public void Equality_SameValues_AreEqual()
    {
        var first = Error.Validation("Test.Code", "Test message.");
        var second = Error.Validation("Test.Code", "Test message.");

        first.Should().Be(second);
        (first == second).Should().BeTrue();
    }

    [Fact]
    public void Equality_DifferentCodes_AreNotEqual()
    {
        var first = Error.Validation("Test.First", "Test message.");
        var second = Error.Validation("Test.Second", "Test message.");

        first.Should().NotBe(second);
        (first != second).Should().BeTrue();
    }

    [Fact]
    public void Equality_SameCodeAndMessageButDifferentType_AreNotEqual()
    {
        var notFound = Error.NotFound("Test.Code", "Test message.");
        var conflict = Error.Conflict("Test.Code", "Test message.");

        notFound.Should().NotBe(conflict);
    }
}
