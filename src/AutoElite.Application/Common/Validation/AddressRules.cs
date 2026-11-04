using FluentValidation;

namespace AutoElite.Application.Common.Validation;

/// <summary>Rules for the parts of an <see cref="Address"/>, each reported on its own field.</summary>
public static class AddressRules
{
    // A valid value for the part that a rule does not check, so the factory reports only the checked part.
    private const string ValidPart = "A";

    /// <summary>Checks the city with <see cref="Address.Create"/>.</summary>
    /// <typeparam name="T">The type of the validated object.</typeparam>
    /// <param name="ruleBuilder">The rule builder of the city.</param>
    public static IRuleBuilderOptionsConditions<T, string?> ValidCity<T>(this IRuleBuilder<T, string?> ruleBuilder) =>
        ruleBuilder.ValidatedBy(city => Address.Create(city, ValidPart));

    /// <summary>Checks the street with <see cref="Address.Create"/>.</summary>
    /// <typeparam name="T">The type of the validated object.</typeparam>
    /// <param name="ruleBuilder">The rule builder of the street.</param>
    public static IRuleBuilderOptionsConditions<T, string?> ValidStreet<T>(this IRuleBuilder<T, string?> ruleBuilder) =>
        ruleBuilder.ValidatedBy(street => Address.Create(ValidPart, street));
}
