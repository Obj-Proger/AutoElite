using System.Globalization;

namespace AutoElite.Domain.ValueObjects;

/// <summary>An amount of money in a specific <see cref="ValueObjects.Currency"/>.</summary>
/// <remarks>
/// <para>
/// Amounts may be negative (for example, a balance in the student's favour); entities that require a
/// positive amount, such as a price or a payment, check it themselves.
/// </para>
/// <para>
/// There is no currency conversion. Arithmetic and comparison are defined only for amounts in the same
/// currency: mixing currencies throws <see cref="InvalidOperationException"/>, because it is a bug in the
/// caller. An operation that receives an amount from outside checks <see cref="HasSameCurrencyAs"/> first
/// and reports a domain error.
/// </para>
/// </remarks>
public sealed class Money : ValueObject
{
    /// <summary>
    /// The largest absolute amount, in major units. Far above any real school payment, and small enough for
    /// the database column (<c>numeric(18, 2)</c>).
    /// </summary>
    public const decimal MaxAbsoluteAmount = 999_999_999_999.99m;

    /// <summary>Gets the amount in major units (for example roubles, not kopecks).</summary>
    public decimal Amount { get; }

    /// <summary>Gets the currency of the amount.</summary>
    public Currency Currency { get; }

    private Money(decimal amount, Currency currency)
    {
        Amount = amount;
        Currency = currency;
    }

    /// <summary>Validates and creates an amount of money.</summary>
    /// <param name="amount">The amount in major units.</param>
    /// <param name="currency">The validated currency.</param>
    /// <returns>
    /// The amount, or a failure if it has more decimal places than the currency allows (it is never rounded
    /// silently) or exceeds <see cref="MaxAbsoluteAmount"/>.
    /// </returns>
    public static Result<Money> Create(decimal amount, Currency currency)
    {
        if (decimal.Round(amount, currency.MinorUnits) != amount)
            return Result.Failure<Money>(DomainErrors.Money.TooManyDecimalPlaces);

        if (Math.Abs(amount) > MaxAbsoluteAmount)
            return Result.Failure<Money>(DomainErrors.Money.TooLarge);

        return new Money(amount, currency);
    }

    /// <summary>Creates a zero amount in the specified currency.</summary>
    public static Money Zero(Currency currency) => new(0m, currency);

    /// <summary>Gets a value indicating whether the amount is zero.</summary>
    public bool IsZero => Amount == 0m;

    /// <summary>Gets a value indicating whether the amount is greater than zero.</summary>
    public bool IsPositive => Amount > 0m;

    /// <summary>Gets a value indicating whether the amount is less than zero.</summary>
    public bool IsNegative => Amount < 0m;

    /// <summary>Determines whether <paramref name="other"/> is in the same currency as this amount.</summary>
    public bool HasSameCurrencyAs(Money other) => Currency == other.Currency;

    /// <summary>Adds two amounts in the same currency.</summary>
    /// <exception cref="InvalidOperationException">The currencies differ.</exception>
    /// <exception cref="OverflowException">The result exceeds <see cref="MaxAbsoluteAmount"/>.</exception>
    public static Money operator +(Money left, Money right)
    {
        EnsureSameCurrency(left, right);

        return Checked(left.Amount + right.Amount, left.Currency);
    }

    /// <summary>Subtracts one amount from another in the same currency.</summary>
    /// <exception cref="InvalidOperationException">The currencies differ.</exception>
    /// <exception cref="OverflowException">The result exceeds <see cref="MaxAbsoluteAmount"/>.</exception>
    public static Money operator -(Money left, Money right)
    {
        EnsureSameCurrency(left, right);

        return Checked(left.Amount - right.Amount, left.Currency);
    }

    /// <summary>Returns the amount with the opposite sign.</summary>
    public static Money operator -(Money money) => new(-money.Amount, money.Currency);

    /// <summary>Determines whether the left amount is less than the right one.</summary>
    /// <exception cref="InvalidOperationException">The currencies differ.</exception>
    public static bool operator <(Money left, Money right) => Compare(left, right) < 0;

    /// <summary>Determines whether the left amount is greater than the right one.</summary>
    /// <exception cref="InvalidOperationException">The currencies differ.</exception>
    public static bool operator >(Money left, Money right) => Compare(left, right) > 0;

    /// <summary>Determines whether the left amount is less than or equal to the right one.</summary>
    /// <exception cref="InvalidOperationException">The currencies differ.</exception>
    public static bool operator <=(Money left, Money right) => Compare(left, right) <= 0;

    /// <summary>Determines whether the left amount is greater than or equal to the right one.</summary>
    /// <exception cref="InvalidOperationException">The currencies differ.</exception>
    public static bool operator >=(Money left, Money right) => Compare(left, right) >= 0;

    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Amount;
        yield return Currency;
    }

    /// <summary>Formats the amount with the currency's decimal places and code, for example <c>1500.00 RUB</c>.</summary>
    public override string ToString() =>
        $"{Amount.ToString("F" + Currency.MinorUnits.ToString(CultureInfo.InvariantCulture), CultureInfo.InvariantCulture)} {Currency.Code}";

    private static int Compare(Money left, Money right)
    {
        EnsureSameCurrency(left, right);

        return left.Amount.CompareTo(right.Amount);
    }

    private static void EnsureSameCurrency(Money left, Money right)
    {
        if (!left.HasSameCurrencyAs(right))
            throw new InvalidOperationException(
                $"Cannot combine amounts in different currencies: {left.Currency.Code} and {right.Currency.Code}.");
    }

    private static Money Checked(decimal amount, Currency currency) =>
        Math.Abs(amount) > MaxAbsoluteAmount
            ? throw new OverflowException($"The amount exceeds the maximum of {MaxAbsoluteAmount.ToString(CultureInfo.InvariantCulture)}.")
            : new Money(amount, currency);
}
