namespace AutoElite.Domain.Common;

/// <summary>
/// Shared validation helpers for the factory methods of entities and value objects.
/// Each helper returns a <see cref="Result{TValue}"/> instead of throwing, so that
/// expected validation failures flow through the domain as values.
/// </summary>
internal static class Guard
{
    /// <summary>Validates that a string is non-empty (after trimming) and within a maximum length.</summary>
    public static Result<string> RequiredText(string? value, int maxLength, Error emptyError, Error tooLongError)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Result.Failure<string>(emptyError);

        var trimmed = value.Trim();

        return trimmed.Length > maxLength
            ? Result.Failure<string>(tooLongError)
            : trimmed;
    }

    /// <summary>
    /// Validates optional text. A null, empty, or whitespace-only value is valid and is normalized to
    /// <see langword="null"/>; otherwise the value is trimmed and must not exceed the maximum length.
    /// </summary>
    public static Result<string?> OptionalText(string? value, int maxLength, Error tooLongError)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Result.Success<string?>(null);

        var trimmed = value.Trim();

        return trimmed.Length > maxLength
            ? Result.Failure<string?>(tooLongError)
            : trimmed;
    }

    /// <summary>Validates that a referenced identifier is not <see cref="Guid.Empty"/>.</summary>
    public static Result<Guid> NotEmpty(Guid value, Error error) =>
        value == Guid.Empty ? Result.Failure<Guid>(error) : value;

    /// <summary>Validates that a UTC instant is strictly later than the current time.</summary>
    public static Result<DateTime> NotPast(DateTime value, DateTime utcNow, Error error) =>
        value <= utcNow ? Result.Failure<DateTime>(error) : value;

    /// <summary>
    /// Validates that an enum value is one of the members declared by its type.
    /// Rejects both out-of-range values (for example <c>(MyEnum)99</c>) and the uninitialized
    /// default, provided the enum does not declare a member with the value zero.
    /// </summary>
    /// <typeparam name="TEnum">The enum type to validate against.</typeparam>
    public static Result<TEnum> DefinedEnum<TEnum>(TEnum value, Error error) where TEnum : struct, Enum =>
        Enum.IsDefined(value) ? value : Result.Failure<TEnum>(error);
}
