using AutoElite.Application.Common.Validation;
using FluentValidation;

namespace AutoElite.Application.Analytics.GetContractSummary;

/// <summary>Checks the period, the branch and the course.</summary>
internal sealed class GetContractSummaryQueryValidator : ReportQueryValidator<GetContractSummaryQuery>
{
    public GetContractSummaryQueryValidator()
    {
        RuleFor(query => query.CourseId)
            .NotEqual(Guid.Empty)
            .WithError(ApplicationErrors.Report.CourseIdEmpty);
    }
}
