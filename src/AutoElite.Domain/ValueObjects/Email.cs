using System.Text.RegularExpressions;

namespace AutoElite.Domain.ValueObjects;

/// <summary>A validated email address.</summary>
public sealed partial class Email : ValueObject
{
    /// <summary>The longest address: the RFC 5321 limit of a mailbox.</summary>
    public const int MaxLength = 320;

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")]
    private static partial Regex Pattern();

    /// <summary>Gets the normalized (trimmed, lowercased) email address.</summary>
    public string Value { get; }

    private Email(string value)
    {
        Value = value;
    }

    /// <summary>Validates and creates an <see cref="Email"/> from raw input.</summary>
    public static Result<Email> Create(string? rawValue)
    {
        if (string.IsNullOrWhiteSpace(rawValue))
            return Result.Failure<Email>(DomainErrors.Email.Empty);

        var trimmed = rawValue.Trim();

        if (trimmed.Length > MaxLength)
            return Result.Failure<Email>(DomainErrors.Email.TooLong);

        if (!Pattern().IsMatch(trimmed))
            return Result.Failure<Email>(DomainErrors.Email.InvalidFormat);

        return new Email(trimmed.ToLowerInvariant());
    }

    /// <inheritdoc />
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    /// <inheritdoc />
    public override string ToString() => Value;
}
