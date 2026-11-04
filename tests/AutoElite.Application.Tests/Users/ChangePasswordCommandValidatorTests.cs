using AutoElite.Application.Users.ChangePassword;

namespace AutoElite.Application.Tests.Users;

public class ChangePasswordCommandValidatorTests
{
    private readonly ChangePasswordCommandValidator _validator = new();

    private string[] ErrorCodes(string? currentPassword, string? newPassword) =>
        [.. _validator.Validate(new ChangePasswordCommand(currentPassword, newPassword)).Errors.Select(failure => failure.ErrorCode)];

    [Fact]
    public void Validate_DifferentValidPasswords_Passes()
    {
        ErrorCodes("old-secret-123", "new-secret-456").Should().BeEmpty();
    }

    [Fact]
    public void Validate_NoCurrentPassword_ReportsPasswordEmpty()
    {
        ErrorCodes(null, "new-secret-456").Should().Equal(ApplicationErrors.Password.Empty.Code);
    }

    [Fact]
    public void Validate_SamePasswords_ReportsSameAsCurrent()
    {
        ErrorCodes("secret-123", "secret-123").Should().Equal(ApplicationErrors.Password.SameAsCurrent.Code);
    }

    [Fact]
    public void Validate_WeakNewPassword_ReportsThePolicyError()
    {
        ErrorCodes("old-secret-123", "short1").Should().Equal(ApplicationErrors.Password.TooShort.Code);
    }
}
