namespace AutoElite.Domain.Tests.ValueObjects;

public class CurrencyTests
{
    [Theory]
    [InlineData("RUB", "RUB")]
    [InlineData("usd", "USD")]
    [InlineData("  Eur  ", "EUR")]
    public void Create_SupportedCode_ReturnsNormalizedCode(string rawCode, string expectedCode)
    {
        var result = Currency.Create(rawCode);

        result.IsSuccess.Should().BeTrue();
        result.Value.Code.Should().Be(expectedCode);
    }

    [Theory]
    [InlineData("RUB", 2)]
    [InlineData("KZT", 2)]
    [InlineData("JPY", 0)]
    public void MinorUnits_ReturnsDecimalPlacesOfCurrency(string code, int expectedMinorUnits)
    {
        var currency = Currency.Create(code).Value;

        currency.MinorUnits.Should().Be(expectedMinorUnits);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_NullEmptyOrWhitespace_ReturnsEmptyError(string? rawCode)
    {
        var result = Currency.Create(rawCode);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DomainErrors.Currency.Empty);
    }

    [Theory]
    [InlineData("RU")]
    [InlineData("RUBL")]
    [InlineData("R1B")]
    [InlineData("РУБ")]
    public void Create_NotThreeLatinLetters_ReturnsInvalidFormatError(string rawCode)
    {
        var result = Currency.Create(rawCode);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DomainErrors.Currency.InvalidFormat);
    }

    [Fact]
    public void Create_UnknownCode_ReturnsNotSupportedError()
    {
        var result = Currency.Create("XYZ");

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DomainErrors.Currency.NotSupported);
    }

    [Fact]
    public void Equals_SameCodeInDifferentCase_AreEqual()
    {
        var first = Currency.Create("rub").Value;
        var second = Currency.Create("RUB").Value;

        first.Should().Be(second);
    }

    [Fact]
    public void SupportedCodes_ContainsRouble()
    {
        Currency.SupportedCodes.Should().Contain("RUB");
    }

    [Fact]
    public void ToString_ReturnsCode()
    {
        var currency = Currency.Create("rub").Value;

        currency.ToString().Should().Be("RUB");
    }
}
