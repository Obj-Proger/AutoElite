using AutoElite.Application.Users.AcceptInvitation;

namespace AutoElite.Application.Tests.Users;

public class AcceptInvitationCommandValidatorTests
{
    private static readonly AcceptInvitationCommand _valid = new(Guid.CreateVersion7(), "invitation-1", "secret-123");

    private readonly AcceptInvitationCommandValidator _validator = new();

    private string[] ErrorCodes(AcceptInvitationCommand command) =>
        [.. _validator.Validate(command).Errors.Select(failure => failure.ErrorCode)];

    [Fact]
    public void Validate_ValidCommand_Passes()
    {
        _validator.Validate(_valid).IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_EmptyUserId_ReportsInvitationInvalid()
    {
        ErrorCodes(_valid with { UserId = Guid.Empty }).Should().Equal(ApplicationErrors.Account.InvitationInvalid.Code);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Validate_NoToken_ReportsInvitationInvalid(string? token)
    {
        ErrorCodes(_valid with { Token = token }).Should().Equal(ApplicationErrors.Account.InvitationInvalid.Code);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Validate_NoPassword_ReportsOnlyPasswordEmpty(string? password)
    {
        ErrorCodes(_valid with { Password = password }).Should().Equal(ApplicationErrors.Password.Empty.Code);
    }

    [Fact]
    public void Validate_ShortPassword_ReportsPasswordTooShort()
    {
        ErrorCodes(_valid with { Password = "abc1234" }).Should().Equal(ApplicationErrors.Password.TooShort.Code);
    }

    [Fact]
    public void Validate_LongPassword_ReportsPasswordTooLong()
    {
        ErrorCodes(_valid with { Password = "a1" + new string('x', 127) }).Should().Equal(ApplicationErrors.Password.TooLong.Code);
    }

    [Fact]
    public void Validate_PasswordWithoutLetter_ReportsLetterRequired()
    {
        ErrorCodes(_valid with { Password = "12345678" }).Should().Equal(ApplicationErrors.Password.LetterRequired.Code);
    }

    [Fact]
    public void Validate_PasswordWithoutDigit_ReportsDigitRequired()
    {
        ErrorCodes(_valid with { Password = "пароль-без-цифр" }).Should().Equal(ApplicationErrors.Password.DigitRequired.Code);
    }

    [Theory]
    [InlineData("abcdefg1")]
    [InlineData("Пароль2026")]
    public void Validate_PasswordAtTheRules_Passes(string password)
    {
        _validator.Validate(_valid with { Password = password }).IsValid.Should().BeTrue();
    }
}
