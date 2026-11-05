using AutoElite.Application.Common.Validation;
using FluentValidation;

namespace AutoElite.Application.Analytics.ExportReport;

/// <summary>Checks the report, the format, the period and the filters.</summary>
internal sealed class ExportReportQueryValidator : ReportQueryValidator<ExportReportQuery>
{
    public ExportReportQueryValidator()
    {
        RuleFor(query => query.Kind)
            .IsInEnum()
            .WithError(ApplicationErrors.Report.KindNotDefined);

        RuleFor(query => query.Format)
            .IsInEnum()
            .WithError(ApplicationErrors.Report.FormatNotDefined);

        RuleFor(query => query.CourseId)
            .NotEqual(Guid.Empty)
            .WithError(ApplicationErrors.Report.CourseIdEmpty);
    }
}
