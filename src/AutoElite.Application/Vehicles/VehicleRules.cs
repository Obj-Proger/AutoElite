using AutoElite.Application.Common.Validation;
using FluentValidation;

namespace AutoElite.Application.Vehicles;

/// <summary>
/// Rules shared by the commands that register and change a vehicle. The make, the model and the year are checked by
/// the domain, whose error codes name the field.
/// </summary>
internal static class VehicleRules
{
    /// <summary>Checks the licence plate with <see cref="LicensePlate.Create"/>.</summary>
    /// <typeparam name="T">The type of the validated command.</typeparam>
    /// <param name="ruleBuilder">The rule builder of the plate.</param>
    public static IRuleBuilderOptionsConditions<T, string?> ValidPlate<T>(this IRuleBuilder<T, string?> ruleBuilder) =>
        ruleBuilder.ValidatedBy(LicensePlate.Create);

    /// <summary>Checks that the category is a licence category.</summary>
    /// <typeparam name="T">The type of the validated command.</typeparam>
    /// <param name="ruleBuilder">The rule builder of the category.</param>
    public static IRuleBuilderOptions<T, LicenseCategory> ValidCategory<T>(this IRuleBuilder<T, LicenseCategory> ruleBuilder) =>
        ruleBuilder.IsInEnum().WithError(DomainErrors.Vehicle.CategoryNotDefined);

    /// <summary>Checks that the transmission is a gearbox type.</summary>
    /// <typeparam name="T">The type of the validated command.</typeparam>
    /// <param name="ruleBuilder">The rule builder of the transmission.</param>
    public static IRuleBuilderOptions<T, Transmission> ValidTransmission<T>(this IRuleBuilder<T, Transmission> ruleBuilder) =>
        ruleBuilder.IsInEnum().WithError(DomainErrors.Vehicle.TransmissionNotDefined);
}
