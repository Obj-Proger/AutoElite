using FluentValidation;
using FluentValidation.Results;

namespace AutoElite.Application.Common.Validation;

/// <summary>Extensions that let FluentValidation rules reuse the validation of domain types.</summary>
public static class RuleBuilderExtensions
{
    /// <summary>
    /// Validates a property by running a domain factory (for example <c>Email.Create</c>) and, if the factory fails,
    /// reporting its error code and message for the property. The domain remains the only place where the rule is defined.
    /// </summary>
    /// <typeparam name="T">The type of the validated object.</typeparam>
    /// <typeparam name="TProperty">The type of the validated property.</typeparam>
    /// <typeparam name="TValue">The type produced by the factory on success.</typeparam>
    /// <param name="ruleBuilder">The rule builder.</param>
    /// <param name="factory">The domain factory that validates and creates the value.</param>
    public static IRuleBuilderOptionsConditions<T, TProperty> ValidatedBy<T, TProperty, TValue>(
        this IRuleBuilder<T, TProperty> ruleBuilder,
        Func<TProperty, Result<TValue>> factory) =>
        ruleBuilder.Custom((value, context) =>
        {
            var result = factory(value);
            if (result.IsFailure)
            {
                context.AddFailure(new ValidationFailure(context.PropertyPath, result.Error.Message)
                {
                    ErrorCode = result.Error.Code
                });
            }
        });

    /// <summary>Reports the code and message of an application or domain error when the rule fails.</summary>
    /// <typeparam name="T">The type of the validated object.</typeparam>
    /// <typeparam name="TProperty">The type of the validated property.</typeparam>
    /// <param name="rule">The rule to describe.</param>
    /// <param name="error">The error whose code and message the failure carries.</param>
    public static IRuleBuilderOptions<T, TProperty> WithError<T, TProperty>(this IRuleBuilderOptions<T, TProperty> rule, Error error)
    {
        ArgumentNullException.ThrowIfNull(rule);
        ArgumentNullException.ThrowIfNull(error);

        return rule.WithErrorCode(error.Code).WithMessage(error.Message);
    }
}
