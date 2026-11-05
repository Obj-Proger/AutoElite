using AutoElite.Application.Common.Validation;
using FluentValidation;

namespace AutoElite.Application.StaffTasks.UpdateStaffTask;

/// <summary>Checks the deadline and the kind of the linked record; the texts are the domain's rules.</summary>
internal sealed class UpdateStaffTaskCommandValidator : AbstractValidator<UpdateStaffTaskCommand>
{
    public UpdateStaffTaskCommandValidator()
    {
        RuleFor(command => command.DueAtUtc).MustBeUtc();

        RuleFor(command => command.SubjectKind)
            .IsInEnum()
            .WithError(DomainErrors.EntityReference.KindNotDefined);
    }
}
