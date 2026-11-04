using AutoElite.Application.Users.Login;

namespace AutoElite.Application.Tests.Users;

public class LoginCommandValidatorTests
{
    private readonly LoginCommandValidator _validator = new();

    [Fact]
    public void Validate_FilledFields_PassesEvenWithoutEmailFormat()
    {
        _validator.Validate(new LoginCommand("not-an-email", "x")).IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_EmptyFields_ReportsBoth()
    {
        var codes = _validator.Validate(new LoginCommand("", null)).Errors.Select(failure => failure.ErrorCode);

        codes.Should().Equal(DomainErrors.Email.Empty.Code, ApplicationErrors.Password.Empty.Code);
    }
}
