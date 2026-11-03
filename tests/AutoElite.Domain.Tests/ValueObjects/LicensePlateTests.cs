namespace AutoElite.Domain.Tests.ValueObjects;

public class LicensePlateTests
{
    [Theory]
    [InlineData("А123ВС96", "А123ВС96")]
    [InlineData("а 123 вс 96", "А123ВС96")]
    [InlineData("  ab-1234  ", "AB1234")]
    public void Create_ValidPlate_ReturnsNormalizedPlate(string rawValue, string expected)
    {
        var result = LicensePlate.Create(rawValue);

        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be(expected);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_NullEmptyOrWhitespace_ReturnsEmptyError(string? rawValue)
    {
        var result = LicensePlate.Create(rawValue);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DomainErrors.LicensePlate.Empty);
    }

    [Theory]
    [InlineData("A")]
    [InlineData("A123BC96A123BC96")]
    [InlineData("A123/BC")]
    [InlineData("--")]
    public void Create_InvalidPlate_ReturnsInvalidFormatError(string rawValue)
    {
        var result = LicensePlate.Create(rawValue);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DomainErrors.LicensePlate.InvalidFormat);
    }

    [Fact]
    public void Create_PlateAtMaximumLength_ReturnsPlate()
    {
        var result = LicensePlate.Create("A123BC96A123BC9");

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Equals_SamePlateTypedDifferently_AreEqual()
    {
        LicensePlate.Create("а123 вс-96").Value.Should().Be(LicensePlate.Create("А123ВС96").Value);
    }
}
