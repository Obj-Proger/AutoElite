using AutoElite.Application.Users.InviteUser;
using AutoElite.Domain.Enums;

namespace AutoElite.Application.Tests.Users;

public class InviteUserCommandValidatorTests
{
    private readonly InviteUserCommandValidator _validator = new();

    private static readonly InviteUserCommand _valid = new(
        "Petrov", "Petr", null, "petr@example.com", null, null, [UserRole.Student]);

    private FluentValidation.Results.ValidationFailure SingleFailure(InviteUserCommand command) =>
        _validator.Validate(command).Errors.Should().ContainSingle().Which;

    [Fact]
    public void Validate_ValidCommand_Passes()
    {
        _validator.Validate(_valid).IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_EveryNamePartInvalid_ReportsEachOnItsOwnField()
    {
        var command = _valid with { LastName = " ", FirstName = null, MiddleName = new string('a', 101) };

        var failures = _validator.Validate(command).Errors;

        failures.Select(failure => (failure.PropertyName, failure.ErrorCode)).Should().BeEquivalentTo(new[]
        {
            ("LastName", DomainErrors.FullName.LastNameEmpty.Code),
            ("FirstName", DomainErrors.FullName.FirstNameEmpty.Code),
            ("MiddleName", DomainErrors.FullName.MiddleNameTooLong.Code)
        });
    }

    [Fact]
    public void Validate_InvalidEmail_ReportsEmailFormat()
    {
        var failure = SingleFailure(_valid with { Email = "not-an-email" });

        failure.PropertyName.Should().Be("Email");
        failure.ErrorCode.Should().Be(DomainErrors.Email.InvalidFormat.Code);
    }

    [Fact]
    public void Validate_InvalidPhone_ReportsPhoneFormat()
    {
        var failure = SingleFailure(_valid with { Phone = "8 999" });

        failure.PropertyName.Should().Be("Phone");
        failure.ErrorCode.Should().Be(DomainErrors.PhoneNumber.InvalidFormat.Code);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void Validate_NoPhone_Passes(string? phone)
    {
        _validator.Validate(_valid with { Phone = phone }).IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_EmptyBranchId_ReportsBranchIdEmpty()
    {
        SingleFailure(_valid with { BranchId = Guid.Empty }).ErrorCode.Should().Be(DomainErrors.User.BranchIdEmpty.Code);
    }

    [Fact]
    public void Validate_NoRoles_ReportsRolesEmpty()
    {
        SingleFailure(_valid with { Roles = [] }).ErrorCode.Should().Be(ApplicationErrors.Account.RolesEmpty.Code);
    }

    [Fact]
    public void Validate_RepeatedRole_ReportsDuplicateRoles()
    {
        var failure = SingleFailure(_valid with { Roles = [UserRole.Student, UserRole.Student] });

        failure.ErrorCode.Should().Be(ApplicationErrors.Account.DuplicateRoles.Code);
    }

    [Fact]
    public void Validate_UndefinedRole_ReportsRoleNotDefined()
    {
        var failure = SingleFailure(_valid with { Roles = [(UserRole)99] });

        failure.ErrorCode.Should().Be(DomainErrors.UserRoleAssignment.RoleNotDefined.Code);
    }

    [Fact]
    public void Validate_UndefinedLanguage_ReportsLanguageNotDefined()
    {
        var failure = SingleFailure(_valid with { PreferredLanguage = (Language)99 });

        failure.ErrorCode.Should().Be(DomainErrors.User.LanguageNotDefined.Code);
    }
}
