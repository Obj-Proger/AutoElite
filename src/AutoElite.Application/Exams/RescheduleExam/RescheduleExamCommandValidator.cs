using AutoElite.Application.Common.Validation;
using FluentValidation;

namespace AutoElite.Application.Exams.RescheduleExam;

/// <summary>Checks that the new time is in UTC; the rest is checked by the domain.</summary>
internal sealed class RescheduleExamCommandValidator : AbstractValidator<RescheduleExamCommand>
{
    public RescheduleExamCommandValidator()
    {
        RuleFor(command => command.ScheduledAtUtc)
            .Must(time => time.Kind == DateTimeKind.Utc)
            .WithError(DomainErrors.TimeInterval.NotUtc);
    }
}
