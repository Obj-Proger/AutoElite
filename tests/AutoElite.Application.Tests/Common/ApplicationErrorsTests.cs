namespace AutoElite.Application.Tests.Common;

public class ApplicationErrorsTests
{
    [Fact]
    public void ValidationFailed_CreatesValidationErrorCarryingFieldErrors()
    {
        FieldError[] fieldErrors = [new("Name", "Test.NameEmpty", "Name is required.")];

        var error = ApplicationErrors.ValidationFailed(fieldErrors);

        error.Code.Should().Be("Validation.Failed");
        error.Message.Should().Be("One or more validation errors occurred.");
        error.Type.Should().Be(ErrorType.Validation);
        error.FieldErrors.Should().Equal(fieldErrors);
    }

    [Fact]
    public void InvalidFieldCode_HasExpectedValue()
    {
        ApplicationErrors.InvalidFieldCode.Should().Be("Validation.Invalid");
    }

    [Fact]
    public void NotFound_UsesTheEntityNameInCodeAndMessage()
    {
        var error = ApplicationErrors.NotFound<TestEntity>();

        error.Code.Should().Be("TestEntity.NotFound");
        error.Message.Should().Be("TestEntity was not found.");
        error.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public void Unauthenticated_IsAnUnauthorizedError()
    {
        var error = ApplicationErrors.Auth.Unauthenticated;

        error.Code.Should().Be("Auth.Unauthenticated");
        error.Type.Should().Be(ErrorType.Unauthorized);
    }

    [Fact]
    public void Forbidden_IsAForbiddenError()
    {
        var error = ApplicationErrors.Auth.Forbidden;

        error.Code.Should().Be("Auth.Forbidden");
        error.Type.Should().Be(ErrorType.Forbidden);
    }
}
