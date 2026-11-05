using AutoElite.Application.Common.Validation;
using FluentValidation;

namespace AutoElite.Application.StaffTasks.CreateStaffTask;

/// <summary>Checks the assignee, the deadline, the linked record and the recurrence; the texts are the domain's rules.</summary>
internal sealed class CreateStaffTaskCommandValidator : AbstractValidator<CreateStaffTaskCommand>
{
    public CreateStaffTaskCommandValidator()
    {
        RuleFor(command => command.AssigneeId)
            .NotEqual(Guid.Empty)
            .WithError(DomainErrors.StaffTask.AssigneeIdEmpty);

        RuleFor(command => command.DueAtUtc).MustBeUtc();

        RuleFor(command => command.SubjectKind)
            .IsInEnum()
            .WithError(DomainErrors.EntityReference.KindNotDefined);

        RuleFor(command => command.Recurrence)
            .IsInEnum()
            .WithError(DomainErrors.StaffTask.RecurrenceNotDefined);
    }
}
