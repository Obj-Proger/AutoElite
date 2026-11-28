namespace AutoElite.Domain.Tests.Entities;

public class PushDeviceTests
{
    private static readonly DateTime _now = new(2026, 11, 1, 10, 0, 0, DateTimeKind.Utc);
    private static readonly Guid _userId = new("11111111-1111-1111-1111-111111111111");

    [Fact]
    public void Register_KeepsTheTokenForTheUser()
    {
        var device = PushDevice.Register(_userId, "token-1", PushPlatform.Android, _now).Value;

        device.Id.Should().NotBeEmpty();
        device.UserId.Should().Be(_userId);
        device.Token.Should().Be("token-1");
        device.Platform.Should().Be(PushPlatform.Android);
        device.RegisteredAtUtc.Should().Be(_now);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Register_NoToken_ReturnsTokenEmptyError(string? token)
    {
        PushDevice.Register(_userId, token, PushPlatform.Android, _now).Error.Should().Be(DomainErrors.PushDevice.TokenEmpty);
    }

    [Fact]
    public void Register_ATokenTooLong_ReturnsTokenTooLongError()
    {
        var token = new string('a', PushDevice.MaxTokenLength + 1);

        PushDevice.Register(_userId, token, PushPlatform.Android, _now).Error.Should().Be(DomainErrors.PushDevice.TokenTooLong);
    }

    [Fact]
    public void Register_NoUser_ReturnsUserIdEmptyError()
    {
        PushDevice.Register(Guid.Empty, "token", PushPlatform.Android, _now).Error.Should().Be(DomainErrors.PushDevice.UserIdEmpty);
    }

    [Fact]
    public void Register_AnUnknownPlatform_ReturnsPlatformNotDefinedError()
    {
        PushDevice.Register(_userId, "token", (PushPlatform)99, _now).Error.Should().Be(DomainErrors.PushDevice.PlatformNotDefined);
    }

    [Fact]
    public void Reassign_PassesTheDeviceToAnotherUser()
    {
        var device = PushDevice.Register(_userId, "token", PushPlatform.Android, _now).Value;
        var other = Guid.NewGuid();

        device.Reassign(other, _now.AddDays(1)).IsSuccess.Should().BeTrue();

        device.UserId.Should().Be(other);
        device.RegisteredAtUtc.Should().Be(_now.AddDays(1));
    }

    [Fact]
    public void Reassign_NoUser_ReturnsUserIdEmptyError()
    {
        var device = PushDevice.Register(_userId, "token", PushPlatform.Android, _now).Value;

        device.Reassign(Guid.Empty, _now).Error.Should().Be(DomainErrors.PushDevice.UserIdEmpty);
        device.UserId.Should().Be(_userId);
    }
}
