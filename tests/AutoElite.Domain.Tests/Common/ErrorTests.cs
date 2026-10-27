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
    public void None_HasNoFieldErrors()
    {
        Error.None.FieldErrors.Should().BeNull();
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

    [Fact]
    public void Validation_WithoutFieldErrors_HasNullFieldErrors()
    {
        var error = Error.Validation("Test.Code", "Test message.");

        error.FieldErrors.Should().BeNull();
    }

    [Fact]
    public void Validation_WithFieldErrors_ExposesThemAndSetsValidationType()
    {
        var fieldErrors = new[]
        {
            new FieldError("Email", "Email.Empty", "Email address cannot be empty."),
            new FieldError("Phone", "PhoneNumber.InvalidFormat", "Phone number is not valid.")
        };

        var error = Error.Validation("Validation.Failed", "One or more validation errors occurred.", fieldErrors);

        error.Type.Should().Be(ErrorType.Validation);
        error.FieldErrors.Should().Equal(fieldErrors);
    }

    [Fact]
    public void FieldError_SameValues_AreEqual()
    {
        var first = new FieldError("Email", "Email.Empty", "Email address cannot be empty.");
        var second = new FieldError("Email", "Email.Empty", "Email address cannot be empty.");

        first.Should().Be(second);
    }

    [Fact]
    public void Equality_SameFieldErrorsInSeparateLists_AreEqual()
    {
        var first = Error.Validation("Validation.Failed", "Failed.", [new FieldError("Email", "Email.Empty", "Empty.")]);
        var second = Error.Validation("Validation.Failed", "Failed.", [new FieldError("Email", "Email.Empty", "Empty.")]);

        first.Should().Be(second);
        (first == second).Should().BeTrue();
        second.GetHashCode().Should().Be(first.GetHashCode());
    }

    [Fact]
    public void Equality_DifferentFieldErrors_AreNotEqual()
    {
        var first = Error.Validation("Validation.Failed", "Failed.", [new FieldError("Email", "Email.Empty", "Empty.")]);
        var second = Error.Validation("Validation.Failed", "Failed.", [new FieldError("Email", "Email.InvalidFormat", "Invalid.")]);

        first.Should().NotBe(second);
        (first != second).Should().BeTrue();
    }

    [Fact]
    public void Equality_SameFieldErrorsInDifferentOrder_AreNotEqual()
    {
        var emailError = new FieldError("Email", "Email.Empty", "Empty.");
        var phoneError = new FieldError("Phone", "PhoneNumber.Empty", "Empty.");
        var first = Error.Validation("Validation.Failed", "Failed.", [emailError, phoneError]);
        var second = Error.Validation("Validation.Failed", "Failed.", [phoneError, emailError]);

        first.Should().NotBe(second);
    }

    [Fact]
    public void Equality_FieldErrorsOnlyOnOneSide_AreNotEqual()
    {
        var withDetails = Error.Validation("Validation.Failed", "Failed.", [new FieldError("Email", "Email.Empty", "Empty.")]);
        var withoutDetails = Error.Validation("Validation.Failed", "Failed.");

        withDetails.Should().NotBe(withoutDetails);
    }

    [Fact]
    public void Equality_NullAndEmptyFieldErrors_AreEqual()
    {
        var withNull = Error.Validation("Validation.Failed", "Failed.");
        var withEmpty = Error.Validation("Validation.Failed", "Failed.", []);

        withNull.Should().Be(withEmpty);
        withEmpty.GetHashCode().Should().Be(withNull.GetHashCode());
    }
}
