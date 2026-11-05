using FluentValidation;

namespace AutoElite.Application.Common.Validation;

/// <summary>Rules for times given by clients.</summary>
public static class TimeRules
{
    /// <summary>Checks that the time is in UTC, so that it means the same moment on every server.</summary>
    /// <typeparam name="T">The type of the validated object.</typeparam>
    /// <param name="ruleBuilder">The rule builder of the time.</param>
    public static IRuleBuilderOptions<T, DateTime> MustBeUtc<T>(this IRuleBuilder<T, DateTime> ruleBuilder) =>
        ruleBuilder
            .Must(time => time.Kind == DateTimeKind.Utc)
            .WithError(DomainErrors.TimeInterval.NotUtc);
}
