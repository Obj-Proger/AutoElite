namespace AutoElite.Domain.Tests.Entities;

public class BranchTests
{
    private const string TimeZoneId = "Asia/Yekaterinburg";
    private const string OtherTimeZoneId = "Europe/Moscow";

    private static readonly Address _address = Address.Create("Yekaterinburg", "Lenina 1").Value;
    private static readonly Address _otherAddress = Address.Create("Moscow", "Tverskaya 10").Value;
    private static readonly PhoneNumber _phone = PhoneNumber.Create("+73431234567").Value;

    private static Branch CreateBranch(PhoneNumber? phone = null) =>
        Branch.Create("Central", _address, phone, TimeZoneId).Value;

    private static Branch CreateInactiveBranch()
    {
        var branch = CreateBranch();
        branch.Deactivate();

        return branch;
    }

    [Fact]
    public void Create_ValidData_ReturnsActiveBranchWithGivenValues()
    {
        var result = Branch.Create("  Central  ", _address, _phone, $"  {TimeZoneId}  ");

        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("Central");
        result.Value.Address.Should().Be(_address);
        result.Value.Phone.Should().Be(_phone);
        result.Value.TimeZoneId.Should().Be(TimeZoneId);
        result.Value.IsActive.Should().BeTrue();
        result.Value.Id.Should().NotBeEmpty();
    }

    [Fact]
    public void Create_WithoutPhone_ReturnsBranchWithoutPhone()
    {
        var result = Branch.Create("Central", _address, null, TimeZoneId);

        result.IsSuccess.Should().BeTrue();
        result.Value.Phone.Should().BeNull();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_EmptyName_ReturnsNameEmptyError(string? name)
    {
        var result = Branch.Create(name, _address, null, TimeZoneId);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DomainErrors.Branch.NameEmpty);
    }

    [Fact]
    public void Create_NameAtMaximumLength_ReturnsBranch()
    {
        var result = Branch.Create(new string('a', 200), _address, null, TimeZoneId);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Create_NameTooLong_ReturnsNameTooLongError()
    {
        var result = Branch.Create(new string('a', 201), _address, null, TimeZoneId);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DomainErrors.Branch.NameTooLong);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_EmptyTimeZone_ReturnsTimeZoneEmptyError(string? timeZoneId)
    {
        var result = Branch.Create("Central", _address, null, timeZoneId);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DomainErrors.Branch.TimeZoneEmpty);
    }

    [Theory]
    [InlineData("Mars/Olympus")]
    [InlineData("Ekaterinburg Standard Time")]
    public void Create_UnknownOrWindowsTimeZone_ReturnsTimeZoneUnknownError(string timeZoneId)
    {
        var result = Branch.Create("Central", _address, null, timeZoneId);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DomainErrors.Branch.TimeZoneUnknown);
    }

    [Fact]
    public void Create_EmptyNameAndTimeZone_ReportsNameFirst()
    {
        var result = Branch.Create(null, _address, null, null);

        result.Error.Should().Be(DomainErrors.Branch.NameEmpty);
    }

    [Fact]
    public void UpdateDetails_ActiveBranch_ChangesAllDetails()
    {
        var branch = CreateBranch(_phone);

        var result = branch.UpdateDetails("North", _otherAddress, null, OtherTimeZoneId);

        result.IsSuccess.Should().BeTrue();
        branch.Name.Should().Be("North");
        branch.Address.Should().Be(_otherAddress);
        branch.Phone.Should().BeNull();
        branch.TimeZoneId.Should().Be(OtherTimeZoneId);
    }

    [Fact]
    public void UpdateDetails_InvalidTimeZone_ReturnsErrorAndKeepsDetails()
    {
        var branch = CreateBranch(_phone);

        var result = branch.UpdateDetails("North", _otherAddress, null, "Mars/Olympus");

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DomainErrors.Branch.TimeZoneUnknown);
        branch.Name.Should().Be("Central");
        branch.Address.Should().Be(_address);
        branch.Phone.Should().Be(_phone);
        branch.TimeZoneId.Should().Be(TimeZoneId);
    }

    [Fact]
    public void UpdateDetails_InactiveBranch_ReturnsCannotModifyInactiveError()
    {
        var branch = CreateInactiveBranch();

        var result = branch.UpdateDetails("North", _otherAddress, null, OtherTimeZoneId);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DomainErrors.Branch.CannotModifyInactive);
        branch.Name.Should().Be("Central");
    }

    [Fact]
    public void UpdateDetails_InactiveBranchAndEmptyName_ReportsInactiveFirst()
    {
        var branch = CreateInactiveBranch();

        var result = branch.UpdateDetails(null, _otherAddress, null, OtherTimeZoneId);

        result.Error.Should().Be(DomainErrors.Branch.CannotModifyInactive);
    }

    [Fact]
    public void Deactivate_ActiveBranch_BecomesInactive()
    {
        var branch = CreateBranch();

        var result = branch.Deactivate();

        result.IsSuccess.Should().BeTrue();
        branch.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Deactivate_InactiveBranch_ReturnsAlreadyInactiveError()
    {
        var branch = CreateInactiveBranch();

        var result = branch.Deactivate();

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DomainErrors.Branch.AlreadyInactive);
    }

    [Fact]
    public void Reactivate_InactiveBranch_BecomesActive()
    {
        var branch = CreateInactiveBranch();

        var result = branch.Reactivate();

        result.IsSuccess.Should().BeTrue();
        branch.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Reactivate_ActiveBranch_ReturnsAlreadyActiveError()
    {
        var branch = CreateBranch();

        var result = branch.Reactivate();

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DomainErrors.Branch.AlreadyActive);
    }
}
