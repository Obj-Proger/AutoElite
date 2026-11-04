using AutoElite.Application.Users.ResetPassword;

namespace AutoElite.Application.Tests.Users;

public class ResetPasswordCommandValidatorTests
{
    private readonly ResetPasswordCommandValidator _validator = new();

    private string[] ErrorCodes(ResetPasswordCommand command) =>
        [.. _validator.Validate(command).Errors.Select(failure => failure.ErrorCode)];

    [Fact]
    public void Validate_ValidCommand_Passes()
    {
        ErrorCodes(new ResetPasswordCommand(Guid.CreateVersion7(), "reset-1", "new-secret-456")).Should().BeEmpty();
    }

    [Fact]
    public void Validate_BrokenLink_ReportsPasswordResetInvalid()
    {
        ErrorCodes(new ResetPasswordCommand(Guid.Empty, "", "new-secret-456")).Should().Equal(
            ApplicationErrors.Account.PasswordResetInvalid.Code, ApplicationErrors.Account.PasswordResetInvalid.Code);
    }

    [Fact]
    public void Validate_WeakPassword_ReportsThePolicyError()
    {
        ErrorCodes(new ResetPasswordCommand(Guid.CreateVersion7(), "reset-1", "no-digits-here"))
            .Should().Equal(ApplicationErrors.Password.DigitRequired.Code);
    }
}
