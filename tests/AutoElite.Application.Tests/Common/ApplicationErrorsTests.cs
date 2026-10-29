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
}
