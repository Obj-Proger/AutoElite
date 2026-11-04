using AutoElite.Application.Common.Validation;
using FluentValidation;

namespace AutoElite.Application.Exams.GetExamSchedule;

/// <summary>Checks the kind filter; the period is checked by the handler.</summary>
internal sealed class GetExamScheduleQueryValidator : AbstractValidator<GetExamScheduleQuery>
{
    public GetExamScheduleQueryValidator()
    {
        RuleFor(query => query.Kind)
            .IsInEnum()
            .WithError(DomainErrors.Exam.KindNotDefined);
    }
}
