using FluentValidation;

namespace AutoElite.Application.Common.Validation;

/// <summary>
/// Rules for a person's name and contacts that reuse the domain value objects, so that each name part is reported
/// on its own field.
/// </summary>
public static class PersonRules
{
    // A valid value for the name parts that a rule does not check, so the factory reports only the checked part.
    private const string ValidNamePart = "A";

    /// <summary>Checks a last name with <see cref="FullName.Create"/>.</summary>
    /// <typeparam name="T">The type of the validated object.</typeparam>
    /// <param name="ruleBuilder">The rule builder of the last name.</param>
    public static IRuleBuilderOptionsConditions<T, string?> ValidLastName<T>(this IRuleBuilder<T, string?> ruleBuilder) =>
        ruleBuilder.ValidatedBy(lastName => FullName.Create(lastName, ValidNamePart));

    /// <summary>Checks a first name with <see cref="FullName.Create"/>.</summary>
    /// <typeparam name="T">The type of the validated object.</typeparam>
    /// <param name="ruleBuilder">The rule builder of the first name.</param>
    public static IRuleBuilderOptionsConditions<T, string?> ValidFirstName<T>(this IRuleBuilder<T, string?> ruleBuilder) =>
        ruleBuilder.ValidatedBy(firstName => FullName.Create(ValidNamePart, firstName));

    /// <summary>Checks an optional middle name with <see cref="FullName.Create"/>.</summary>
    /// <typeparam name="T">The type of the validated object.</typeparam>
    /// <param name="ruleBuilder">The rule builder of the middle name.</param>
    public static IRuleBuilderOptionsConditions<T, string?> ValidMiddleName<T>(this IRuleBuilder<T, string?> ruleBuilder) =>
        ruleBuilder.ValidatedBy(middleName => FullName.Create(ValidNamePart, ValidNamePart, middleName));

    /// <summary>Checks an email address with <see cref="Email.Create"/>.</summary>
    /// <typeparam name="T">The type of the validated object.</typeparam>
    /// <param name="ruleBuilder">The rule builder of the email address.</param>
    public static IRuleBuilderOptionsConditions<T, string?> ValidEmail<T>(this IRuleBuilder<T, string?> ruleBuilder) =>
        ruleBuilder.ValidatedBy(Email.Create);

    /// <summary>
    /// Checks a phone number with <see cref="PhoneNumber.Create"/>. For an optional phone, apply it only when a value
    /// is given; the handler treats an empty value as no phone.
    /// </summary>
    /// <typeparam name="T">The type of the validated object.</typeparam>
    /// <param name="ruleBuilder">The rule builder of the phone number.</param>
    public static IRuleBuilderOptionsConditions<T, string?> ValidPhone<T>(this IRuleBuilder<T, string?> ruleBuilder) =>
        ruleBuilder.ValidatedBy(PhoneNumber.Create);
}
