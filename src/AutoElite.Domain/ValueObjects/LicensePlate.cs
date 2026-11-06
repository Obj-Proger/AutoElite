namespace AutoElite.Domain.ValueObjects;

/// <summary>A vehicle registration plate, for example <c>А123ВС96</c>.</summary>
/// <remarks>
/// The value is normalized to upper case without spaces and hyphens, so that the same plate typed differently is
/// one value. Letters of any alphabet are accepted, because plates differ by country; Cyrillic and Latin look-alike
/// letters are not unified, so the school should enter plates consistently.
/// </remarks>
public sealed class LicensePlate : ValueObject
{
    private const int MinLength = 2;
    /// <summary>The longest plate.</summary>
    public const int MaxLength = 15;

    /// <summary>Gets the normalized plate.</summary>
    public string Value { get; }

    private LicensePlate(string value)
    {
        Value = value;
    }

    /// <summary>Validates and creates a <see cref="LicensePlate"/> from raw input.</summary>
    public static Result<LicensePlate> Create(string? rawValue)
    {
        if (string.IsNullOrWhiteSpace(rawValue))
            return Result.Failure<LicensePlate>(DomainErrors.LicensePlate.Empty);

        var normalized = new string(rawValue.Where(character => !char.IsWhiteSpace(character) && character != '-').ToArray())
            .ToUpperInvariant();

        if (normalized.Length is < MinLength or > MaxLength || !normalized.All(char.IsLetterOrDigit))
            return Result.Failure<LicensePlate>(DomainErrors.LicensePlate.InvalidFormat);

        return new LicensePlate(normalized);
    }

    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    /// <inheritdoc />
    public override string ToString() => Value;
}
