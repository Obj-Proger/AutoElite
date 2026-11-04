using AutoElite.Application.Common.Validation;
using FluentValidation;

namespace AutoElite.Application.Branches;

/// <summary>Rules for the name and time zone of a branch that reuse <see cref="Branch.Create"/>.</summary>
internal static class BranchRules
{
    private const string ValidName = "A";
    private const string ValidTimeZoneId = "Europe/Moscow";

    private static readonly Address _validAddress = Address.Create("A", "A").Value;

    /// <summary>Checks a branch name.</summary>
    /// <typeparam name="T">The type of the validated object.</typeparam>
    /// <param name="ruleBuilder">The rule builder of the name.</param>
    public static IRuleBuilderOptionsConditions<T, string?> ValidBranchName<T>(this IRuleBuilder<T, string?> ruleBuilder) =>
        ruleBuilder.ValidatedBy(name => Branch.Create(name, _validAddress, null, ValidTimeZoneId));

    /// <summary>Checks an IANA time zone identifier.</summary>
    /// <typeparam name="T">The type of the validated object.</typeparam>
    /// <param name="ruleBuilder">The rule builder of the time zone.</param>
    public static IRuleBuilderOptionsConditions<T, string?> ValidTimeZone<T>(this IRuleBuilder<T, string?> ruleBuilder) =>
        ruleBuilder.ValidatedBy(timeZoneId => Branch.Create(ValidName, _validAddress, null, timeZoneId));
}
