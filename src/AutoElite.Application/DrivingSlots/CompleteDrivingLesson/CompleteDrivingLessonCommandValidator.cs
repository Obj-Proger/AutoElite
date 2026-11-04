using AutoElite.Application.Common.Validation;
using FluentValidation;

namespace AutoElite.Application.DrivingSlots.CompleteDrivingLesson;

/// <summary>Checks each grade; the hours and repeated skills are checked by the domain.</summary>
internal sealed class CompleteDrivingLessonCommandValidator : AbstractValidator<CompleteDrivingLessonCommand>
{
    public CompleteDrivingLessonCommandValidator()
    {
        RuleForEach(command => command.Grades!)
            .ValidatedBy(input => SkillGrade.Create(input.Skill, input.Grade));
    }
}
