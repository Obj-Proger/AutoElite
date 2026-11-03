using System.Collections.Frozen;

namespace AutoElite.Domain.ValueObjects;

/// <summary>A currency identified by its ISO 4217 alphabetic code, for example <c>RUB</c>.</summary>
/// <remarks>
/// Only the currencies listed in <see cref="SupportedCodes"/> are accepted, because each one must declare
/// how many decimal places (minor units) its amounts may have. Adding a currency means adding it to the list.
/// </remarks>
public sealed class Currency : ValueObject
{
    private const int CodeLength = 3;

    // ISO 4217 code -> number of minor units (decimal places).
    private static readonly FrozenDictionary<string, int> _minorUnitsByCode = new Dictionary<string, int>
    {
        ["RUB"] = 2, ["BYN"] = 2, ["KZT"] = 2, ["UZS"] = 2, ["KGS"] = 2, ["TJS"] = 2, ["TMT"] = 2,
        ["AMD"] = 2, ["AZN"] = 2, ["GEL"] = 2, ["MDL"] = 2, ["UAH"] = 2,
        ["USD"] = 2, ["EUR"] = 2, ["GBP"] = 2, ["CHF"] = 2, ["CNY"] = 2, ["TRY"] = 2, ["AED"] = 2,
        ["JPY"] = 0, ["KRW"] = 0
    }.ToFrozenDictionary(StringComparer.Ordinal);

    /// <summary>Gets the codes of all supported currencies.</summary>
    public static IReadOnlyCollection<string> SupportedCodes => _minorUnitsByCode.Keys;

    /// <summary>Gets the ISO 4217 alphabetic code in upper case.</summary>
    public string Code { get; }

    /// <summary>Gets the number of decimal places an amount in this currency may have.</summary>
    public int MinorUnits => _minorUnitsByCode[Code];

    private Currency(string code)
    {
        Code = code;
    }

    /// <summary>Validates and creates a <see cref="Currency"/> from an ISO 4217 code in any letter case.</summary>
    public static Result<Currency> Create(string? rawCode)
    {
        if (string.IsNullOrWhiteSpace(rawCode))
            return Result.Failure<Currency>(DomainErrors.Currency.Empty);

        var code = rawCode.Trim().ToUpperInvariant();

        if (code.Length != CodeLength || !code.All(char.IsAsciiLetterUpper))
            return Result.Failure<Currency>(DomainErrors.Currency.InvalidFormat);

        if (!_minorUnitsByCode.ContainsKey(code))
            return Result.Failure<Currency>(DomainErrors.Currency.NotSupported);

        return new Currency(code);
    }

    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Code;
    }

    /// <inheritdoc />
    public override string ToString() => Code;
}
