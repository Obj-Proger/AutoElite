using AutoElite.Application.Common.Validation;
using FluentValidation;

namespace AutoElite.Application.TheoryLessons.GetTheorySchedule;

/// <summary>Checks the filters; the period is checked by the handler together with the domain rules of an interval.</summary>
internal sealed class GetTheoryScheduleQueryValidator : AbstractValidator<GetTheoryScheduleQuery>
{
    public GetTheoryScheduleQueryValidator()
    {
        RuleFor(query => query.Status)
            .IsInEnum()
            .WithError(ApplicationErrors.TheoryLesson.StatusNotDefined);

        RuleFor(query => query.BranchId)
            .NotEqual(Guid.Empty)
            .WithError(DomainErrors.StudyGroup.BranchIdEmpty);
    }
}
