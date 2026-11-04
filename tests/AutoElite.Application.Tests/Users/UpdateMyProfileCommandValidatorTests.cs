using AutoElite.Application.Users.UpdateMyProfile;
using AutoElite.Domain.Enums;

namespace AutoElite.Application.Tests.Users;

public class UpdateMyProfileCommandValidatorTests
{
    private static readonly UpdateMyProfileCommand _valid = new("Petrova", "Anna", null, null, Language.Russian);

    private readonly UpdateMyProfileCommandValidator _validator = new();

    private string[] ErrorCodes(UpdateMyProfileCommand command) =>
        [.. _validator.Validate(command).Errors.Select(failure => failure.ErrorCode)];

    [Fact]
    public void Validate_ValidCommand_Passes()
    {
        ErrorCodes(_valid).Should().BeEmpty();
    }

    [Fact]
    public void Validate_EmptyNames_ReportsEachName()
    {
        ErrorCodes(_valid with { LastName = "", FirstName = " " })
            .Should().Equal(DomainErrors.FullName.LastNameEmpty.Code, DomainErrors.FullName.FirstNameEmpty.Code);
    }

    [Fact]
    public void Validate_InvalidPhone_ReportsPhoneFormat()
    {
        ErrorCodes(_valid with { Phone = "12345" }).Should().Equal(DomainErrors.PhoneNumber.InvalidFormat.Code);
    }

    [Fact]
    public void Validate_UndefinedLanguage_ReportsLanguageNotDefined()
    {
        ErrorCodes(_valid with { PreferredLanguage = (Language)99 }).Should().Equal(DomainErrors.User.LanguageNotDefined.Code);
    }
}
