using System.Text.RegularExpressions;

namespace AutoElite.Domain.ValueObjects;

/// <summary>A validated email address.</summary>
public sealed partial class Email : ValueObject
{
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

        if (trimmed.Length > 320) // RFC 5321 maximum mailbox length
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