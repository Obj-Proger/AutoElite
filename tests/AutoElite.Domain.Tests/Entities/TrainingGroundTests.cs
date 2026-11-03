namespace AutoElite.Domain.Tests.Entities;

public class TrainingGroundTests
{
    private static readonly Address _address = Address.Create("Yekaterinburg", "Shefskaya 2").Value;
    private static readonly Address _otherAddress = Address.Create("Yekaterinburg", "Bebelya 100").Value;
    private static readonly Guid _branchId = new("22222222-2222-2222-2222-222222222222");

    private static TrainingGround CreateGround() => TrainingGround.Create("North ground", _address, null, null).Value;

    [Fact]
    public void Create_ValidData_ReturnsActiveGroundWithGivenValues()
    {
        var result = TrainingGround.Create("  North ground  ", _address, "  Closed area with a hill  ", _branchId);

        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("North ground");
        result.Value.Address.Should().Be(_address);
        result.Value.Description.Should().Be("Closed area with a hill");
        result.Value.BranchId.Should().Be(_branchId);
        result.Value.IsActive.Should().BeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("   ")]
    public void Create_EmptyName_ReturnsNameEmptyError(string? name)
    {
        var result = TrainingGround.Create(name, _address, null, null);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DomainErrors.TrainingGround.NameEmpty);
    }

    [Fact]
    public void Create_NameTooLong_ReturnsNameTooLongError()
    {
        var result = TrainingGround.Create(new string('a', 201), _address, null, null);

        result.Error.Should().Be(DomainErrors.TrainingGround.NameTooLong);
    }

    [Fact]
    public void Create_DescriptionTooLong_ReturnsDescriptionTooLongError()
    {
        var result = TrainingGround.Create("North ground", _address, new string('a', 2001), null);

        result.Error.Should().Be(DomainErrors.TrainingGround.DescriptionTooLong);
    }

    [Fact]
    public void Create_EmptyBranchId_ReturnsBranchIdEmptyError()
    {
        var result = TrainingGround.Create("North ground", _address, null, Guid.Empty);

        result.Error.Should().Be(DomainErrors.TrainingGround.BranchIdEmpty);
    }

    [Fact]
    public void UpdateDetails_ActiveGround_ReplacesDetails()
    {
        var ground = CreateGround();

        var result = ground.UpdateDetails("South ground", _otherAddress, "Parking practice", _branchId);

        result.IsSuccess.Should().BeTrue();
        ground.Name.Should().Be("South ground");
        ground.Address.Should().Be(_otherAddress);
        ground.Description.Should().Be("Parking practice");
        ground.BranchId.Should().Be(_branchId);
    }

    [Fact]
    public void UpdateDetails_InvalidName_ReturnsErrorAndKeepsDetails()
    {
        var ground = CreateGround();

        var result = ground.UpdateDetails(null, _otherAddress, null, null);

        result.Error.Should().Be(DomainErrors.TrainingGround.NameEmpty);
        ground.Address.Should().Be(_address);
    }

    [Fact]
    public void UpdateDetails_InactiveGround_ReturnsCannotModifyInactiveError()
    {
        var ground = CreateGround();
        ground.Deactivate();

        var result = ground.UpdateDetails("South ground", _otherAddress, null, null);

        result.Error.Should().Be(DomainErrors.TrainingGround.CannotModifyInactive);
    }

    [Fact]
    public void Deactivate_ThenReactivate_RestoresActiveState()
    {
        var ground = CreateGround();
        ground.Deactivate();

        var result = ground.Reactivate();

        result.IsSuccess.Should().BeTrue();
        ground.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Deactivate_InactiveGround_ReturnsAlreadyInactiveError()
    {
        var ground = CreateGround();
        ground.Deactivate();

        ground.Deactivate().Error.Should().Be(DomainErrors.TrainingGround.AlreadyInactive);
    }

    [Fact]
    public void Reactivate_ActiveGround_ReturnsAlreadyActiveError()
    {
        CreateGround().Reactivate().Error.Should().Be(DomainErrors.TrainingGround.AlreadyActive);
    }
}
