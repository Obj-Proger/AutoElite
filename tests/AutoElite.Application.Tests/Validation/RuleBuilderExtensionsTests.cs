namespace AutoElite.Application.Tests.Validation;

public class RuleBuilderExtensionsTests
{
    private readonly EmailRequestValidator _validator = new();

    [Fact]
    public void ValidatedBy_ValidValue_Passes()
    {
        var result = _validator.Validate(new EmailRequest("ivan@example.com"));

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ValidatedBy_EmptyValue_ReportsDomainEmptyError(string? email)
    {
        var result = _validator.Validate(new EmailRequest(email));

        var failure = result.Errors.Should().ContainSingle().Which;
        failure.PropertyName.Should().Be("Email");
        failure.ErrorCode.Should().Be(DomainErrors.Email.Empty.Code);
        failure.ErrorMessage.Should().Be(DomainErrors.Email.Empty.Message);
    }

    [Fact]
    public void ValidatedBy_InvalidFormat_ReportsDomainFormatError()
    {
        var result = _validator.Validate(new EmailRequest("not-an-email"));

        var failure = result.Errors.Should().ContainSingle().Which;
        failure.PropertyName.Should().Be("Email");
        failure.ErrorCode.Should().Be(DomainErrors.Email.InvalidFormat.Code);
        failure.ErrorMessage.Should().Be(DomainErrors.Email.InvalidFormat.Message);
    }
}
