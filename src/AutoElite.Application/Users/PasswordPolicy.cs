using AutoElite.Application.Common.Validation;
using FluentValidation;

namespace AutoElite.Application.Users;

/// <summary>
/// The rules every password must follow. Commands check them with <see cref="ValidPassword{T}"/>; the infrastructure
/// configures the identity system with the same limits, so both reject the same passwords.
/// </summary>
public static class PasswordPolicy
{
    /// <summary>The shortest allowed password.</summary>
    public const int MinLength = 8;

    /// <summary>The longest allowed password, which keeps hashing cheap.</summary>
    public const int MaxLength = 128;

    /// <summary>
    /// Checks a new password: from <see cref="MinLength"/> to <see cref="MaxLength"/> characters with at least one
    /// letter and one digit. Each broken rule is reported separately.
    /// </summary>
    /// <typeparam name="T">The type of the validated object.</typeparam>
    /// <param name="ruleBuilder">The rule builder of the password.</param>
    public static IRuleBuilderOptions<T, string?> ValidPassword<T>(this IRuleBuilder<T, string?> ruleBuilder)
    {
        ArgumentNullException.ThrowIfNull(ruleBuilder);

        // An empty password breaks only the first rule; the others describe a password that was typed.
        return ruleBuilder
            .NotEmpty().WithError(ApplicationErrors.Password.Empty)
            .Must(password => string.IsNullOrEmpty(password) || password.Length >= MinLength)
            .WithError(ApplicationErrors.Password.TooShort)
            .Must(password => string.IsNullOrEmpty(password) || password.Length <= MaxLength)
            .WithError(ApplicationErrors.Password.TooLong)
            .Must(password => string.IsNullOrEmpty(password) || password.Any(char.IsLetter))
            .WithError(ApplicationErrors.Password.LetterRequired)
            .Must(password => string.IsNullOrEmpty(password) || password.Any(char.IsDigit))
            .WithError(ApplicationErrors.Password.DigitRequired);
    }
}
