using AutoElite.Application.Branches.CreateBranch;

namespace AutoElite.Application.Tests.Branches;

public class CreateBranchCommandValidatorTests
{
    private static readonly CreateBranchCommand _valid = new("Central", "Yekaterinburg", "Lenina 1", null, "Asia/Yekaterinburg");

    private readonly CreateBranchCommandValidator _validator = new();

    private (string Field, string Code)[] Failures(CreateBranchCommand command) =>
        [.. _validator.Validate(command).Errors.Select(failure => (failure.PropertyName, failure.ErrorCode))];

    [Fact]
    public void Validate_ValidCommand_Passes()
    {
        Failures(_valid).Should().BeEmpty();
    }

    [Fact]
    public void Validate_EveryFieldInvalid_ReportsEachOnItsOwnField()
    {
        var command = new CreateBranchCommand(" ", "", new string('a', 301), "123", "Russian Standard Time");

        Failures(command).Should().BeEquivalentTo(new[]
        {
            ("Name", DomainErrors.Branch.NameEmpty.Code),
            ("City", DomainErrors.Address.CityEmpty.Code),
            ("Street", DomainErrors.Address.StreetTooLong.Code),
            ("TimeZoneId", DomainErrors.Branch.TimeZoneUnknown.Code),
            ("Phone", DomainErrors.PhoneNumber.InvalidFormat.Code)
        });
    }

    [Fact]
    public void Validate_NoTimeZone_ReportsTimeZoneEmpty()
    {
        Failures(_valid with { TimeZoneId = null }).Should().Equal(("TimeZoneId", DomainErrors.Branch.TimeZoneEmpty.Code));
    }
}
