using AutoElite.Application.Common.Validation;
using FluentValidation;

namespace AutoElite.Application.TheoryLessons.MarkAttendance;

/// <summary>Checks that there is at least one mark and at most one per contract.</summary>
internal sealed class MarkAttendanceCommandValidator : AbstractValidator<MarkAttendanceCommand>
{
    public MarkAttendanceCommandValidator()
    {
        RuleFor(command => command.Marks)
            .NotEmpty()
            .WithError(ApplicationErrors.TheoryLesson.MarksEmpty);

        RuleFor(command => command.Marks)
            .Must(marks => marks is null || marks.Select(mark => mark.ContractId).Distinct().Count() == marks.Count)
            .WithError(ApplicationErrors.TheoryLesson.DuplicateMark);
    }
}
