using AutoElite.Application.Users.ConfirmEmailChange;
using AutoElite.Application.Users.RequestEmailChange;

namespace AutoElite.Application.Tests.Users;

public class EmailChangeValidatorTests
{
    [Fact]
    public void RequestValidator_InvalidAddressAndNoPassword_ReportsBoth()
    {
        var result = new RequestEmailChangeCommandValidator().Validate(new RequestEmailChangeCommand("not-an-email", ""));

        result.Errors.Select(failure => failure.ErrorCode)
            .Should().Equal(DomainErrors.Email.InvalidFormat.Code, ApplicationErrors.Password.Empty.Code);
    }

    [Fact]
    public void ConfirmValidator_BrokenLink_ReportsEmailChangeInvalidForEachPart()
    {
        var result = new ConfirmEmailChangeCommandValidator().Validate(new ConfirmEmailChangeCommand(Guid.Empty, null, ""));

        result.Errors.Select(failure => failure.ErrorCode)
            .Should().HaveCount(3).And.OnlyContain(code => code == ApplicationErrors.Account.EmailChangeInvalid.Code);
    }

    [Fact]
    public void ConfirmValidator_CompleteLink_Passes()
    {
        var command = new ConfirmEmailChangeCommand(Guid.CreateVersion7(), "ivan@example.com", "email-1");

        new ConfirmEmailChangeCommandValidator().Validate(command).IsValid.Should().BeTrue();
    }
}
