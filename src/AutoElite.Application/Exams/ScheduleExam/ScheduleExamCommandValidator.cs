using AutoElite.Application.Common.Validation;
using FluentValidation;

namespace AutoElite.Application.Exams.ScheduleExam;

/// <summary>Checks the kind and that the time is in UTC; the rest is checked by the domain.</summary>
internal sealed class ScheduleExamCommandValidator : AbstractValidator<ScheduleExamCommand>
{
    public ScheduleExamCommandValidator()
    {
        RuleFor(command => command.Kind)
            .IsInEnum()
            .WithError(DomainErrors.Exam.KindNotDefined);

        RuleFor(command => command.ScheduledAtUtc)
            .Must(time => time.Kind == DateTimeKind.Utc)
            .WithError(DomainErrors.TimeInterval.NotUtc);
    }
}
