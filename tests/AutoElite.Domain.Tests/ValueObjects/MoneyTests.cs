using System.Globalization;

namespace AutoElite.Domain.Tests.ValueObjects;

public class MoneyTests
{
    private static readonly Currency _rub = Currency.Create("RUB").Value;
    private static readonly Currency _usd = Currency.Create("USD").Value;
    private static readonly Currency _jpy = Currency.Create("JPY").Value;

    private static Money Rub(decimal amount) => Money.Create(amount, _rub).Value;

    [Theory]
    [InlineData("1500")]
    [InlineData("1500.5")]
    [InlineData("1500.55")]
    [InlineData("0")]
    [InlineData("-200.10")]
    public void Create_AmountWithinCurrencyPrecision_ReturnsMoney(string rawAmount)
    {
        var amount = decimal.Parse(rawAmount, CultureInfo.InvariantCulture);

        var result = Money.Create(amount, _rub);

        result.IsSuccess.Should().BeTrue();
        result.Value.Amount.Should().Be(amount);
        result.Value.Currency.Should().Be(_rub);
    }

    [Fact]
    public void Create_MoreDecimalPlacesThanCurrencyAllows_ReturnsTooManyDecimalPlacesError()
    {
        var result = Money.Create(10.005m, _rub);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DomainErrors.Money.TooManyDecimalPlaces);
    }

    [Fact]
    public void Create_FractionInCurrencyWithoutMinorUnits_ReturnsTooManyDecimalPlacesError()
    {
        var result = Money.Create(100.5m, _jpy);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DomainErrors.Money.TooManyDecimalPlaces);
    }

    [Fact]
    public void Create_TrailingZerosBeyondPrecision_ReturnsMoney()
    {
        var result = Money.Create(10.500m, _rub);

        result.IsSuccess.Should().BeTrue();
    }

    [Theory]
    [InlineData("999999999999.99")]
    [InlineData("-999999999999.99")]
    public void Create_AmountAtMaximum_ReturnsMoney(string rawAmount)
    {
        var result = Money.Create(decimal.Parse(rawAmount, CultureInfo.InvariantCulture), _rub);

        result.IsSuccess.Should().BeTrue();
    }

    [Theory]
    [InlineData("1000000000000")]
    [InlineData("-1000000000000")]
    public void Create_AmountAboveMaximum_ReturnsTooLargeError(string rawAmount)
    {
        var result = Money.Create(decimal.Parse(rawAmount, CultureInfo.InvariantCulture), _rub);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DomainErrors.Money.TooLarge);
    }

    [Fact]
    public void Zero_ReturnsZeroAmountInCurrency()
    {
        var zero = Money.Zero(_usd);

        zero.Amount.Should().Be(0m);
        zero.Currency.Should().Be(_usd);
        zero.IsZero.Should().BeTrue();
    }

    [Theory]
    [InlineData("10", false, true, false)]
    [InlineData("0", true, false, false)]
    [InlineData("-10", false, false, true)]
    public void SignProperties_ReflectAmount(string rawAmount, bool isZero, bool isPositive, bool isNegative)
    {
        var money = Rub(decimal.Parse(rawAmount, CultureInfo.InvariantCulture));

        money.IsZero.Should().Be(isZero);
        money.IsPositive.Should().Be(isPositive);
        money.IsNegative.Should().Be(isNegative);
    }

    [Fact]
    public void HasSameCurrencyAs_SameCurrency_ReturnsTrue()
    {
        Rub(1m).HasSameCurrencyAs(Rub(2m)).Should().BeTrue();
    }

    [Fact]
    public void HasSameCurrencyAs_DifferentCurrency_ReturnsFalse()
    {
        Rub(1m).HasSameCurrencyAs(Money.Zero(_usd)).Should().BeFalse();
    }

    [Fact]
    public void Add_SameCurrency_ReturnsSum()
    {
        var sum = Rub(1500.50m) + Rub(200.25m);

        sum.Should().Be(Rub(1700.75m));
    }

    [Fact]
    public void Subtract_SameCurrency_ReturnsDifferenceThatMayBeNegative()
    {
        var difference = Rub(100m) - Rub(150.50m);

        difference.Should().Be(Rub(-50.50m));
    }

    [Fact]
    public void Negate_ReturnsAmountWithOppositeSign()
    {
        var negated = -Rub(100m);

        negated.Should().Be(Rub(-100m));
    }

    [Fact]
    public void Add_DifferentCurrencies_Throws()
    {
        var act = () => Rub(1m) + Money.Zero(_usd);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Subtract_DifferentCurrencies_Throws()
    {
        var act = () => Rub(1m) - Money.Zero(_usd);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Add_ResultAboveMaximum_Throws()
    {
        var act = () => Rub(999_999_999_999.99m) + Rub(0.01m);

        act.Should().Throw<OverflowException>();
    }

    [Fact]
    public void ComparisonOperators_SameCurrency_CompareAmounts()
    {
        var smaller = Rub(100m);
        var larger = Rub(200m);

        (smaller < larger).Should().BeTrue();
        (larger > smaller).Should().BeTrue();
        (smaller <= Rub(100m)).Should().BeTrue();
        (larger >= Rub(200m)).Should().BeTrue();
        (larger < smaller).Should().BeFalse();
    }

    [Fact]
    public void ComparisonOperator_DifferentCurrencies_Throws()
    {
        var act = () => Rub(1m) < Money.Zero(_usd);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Equals_SameAmountWithDifferentScale_AreEqual()
    {
        Rub(10.5m).Should().Be(Rub(10.50m));
    }

    [Fact]
    public void Equals_SameAmountInDifferentCurrencies_AreNotEqual()
    {
        Money.Create(10m, _usd).Value.Should().NotBe(Rub(10m));
    }

    [Fact]
    public void ToString_FormatsWithCurrencyDecimalPlacesAndCode()
    {
        Rub(1500m).ToString().Should().Be("1500.00 RUB");
        Money.Create(300m, _jpy).Value.ToString().Should().Be("300 JPY");
    }
}
