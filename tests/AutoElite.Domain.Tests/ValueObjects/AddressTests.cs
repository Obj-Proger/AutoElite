namespace AutoElite.Domain.Tests.ValueObjects;

public class AddressTests
{
    [Fact]
    public void Create_ValidParts_ReturnsTrimmedAddress()
    {
        var result = Address.Create("  Yekaterinburg  ", "  Lenina 1, office 5  ");

        result.IsSuccess.Should().BeTrue();
        result.Value.City.Should().Be("Yekaterinburg");
        result.Value.Street.Should().Be("Lenina 1, office 5");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_EmptyCity_ReturnsCityEmptyError(string? city)
    {
        var result = Address.Create(city, "Lenina 1");

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DomainErrors.Address.CityEmpty);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_EmptyStreet_ReturnsStreetEmptyError(string? street)
    {
        var result = Address.Create("Yekaterinburg", street);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DomainErrors.Address.StreetEmpty);
    }

    [Fact]
    public void Create_PartsAtMaximumLength_ReturnsAddress()
    {
        var result = Address.Create(new string('a', 100), new string('b', 300));

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Create_CityTooLong_ReturnsCityTooLongError()
    {
        var result = Address.Create(new string('a', 101), "Lenina 1");

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DomainErrors.Address.CityTooLong);
    }

    [Fact]
    public void Create_StreetTooLong_ReturnsStreetTooLongError()
    {
        var result = Address.Create("Yekaterinburg", new string('b', 301));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DomainErrors.Address.StreetTooLong);
    }

    [Fact]
    public void Create_EmptyCityAndStreet_ReportsCityFirst()
    {
        var result = Address.Create(null, null);

        result.Error.Should().Be(DomainErrors.Address.CityEmpty);
    }

    [Fact]
    public void Equals_SameParts_AreEqual()
    {
        Address.Create("Yekaterinburg", "Lenina 1").Value.Should().Be(Address.Create("Yekaterinburg", "Lenina 1").Value);
    }

    [Fact]
    public void ToString_JoinsCityAndStreet()
    {
        Address.Create("Yekaterinburg", "Lenina 1").Value.ToString().Should().Be("Yekaterinburg, Lenina 1");
    }
}
