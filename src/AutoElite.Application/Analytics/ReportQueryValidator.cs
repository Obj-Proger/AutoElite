using AutoElite.Application.Common.Validation;
using FluentValidation;

namespace AutoElite.Application.Analytics;

/// <summary>Checks the period and the branch of a report query; derived validators add the query's own rules.</summary>
/// <typeparam name="TQuery">The type of the report query.</typeparam>
public abstract class ReportQueryValidator<TQuery> : AbstractValidator<TQuery>
    where TQuery : IReportQuery
{
    /// <summary>The longest period a report covers, three years.</summary>
    public const int MaxPeriodDays = 1096;

    /// <summary>Adds the rules of the period and the branch.</summary>
    protected ReportQueryValidator()
    {
        RuleFor(query => query.To)
            .GreaterThanOrEqualTo(query => query.From)
            .WithError(ApplicationErrors.Report.PeriodInvalid);

        RuleFor(query => query.To)
            .Must((query, to) => to.DayNumber - query.From.DayNumber < MaxPeriodDays)
            .When(query => query.To >= query.From)
            .WithError(ApplicationErrors.Report.PeriodTooLong);

        RuleFor(query => query.BranchId)
            .NotEqual(Guid.Empty)
            .WithError(ApplicationErrors.Report.BranchIdEmpty);
    }
}
