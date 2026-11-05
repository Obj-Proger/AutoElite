using AutoElite.Application.Common.Validation;
using FluentValidation;

namespace AutoElite.Application.StaffTasks.ReassignStaffTask;

/// <summary>Checks the identifier of the new assignee.</summary>
internal sealed class ReassignStaffTaskCommandValidator : AbstractValidator<ReassignStaffTaskCommand>
{
    public ReassignStaffTaskCommandValidator()
    {
        RuleFor(command => command.AssigneeId)
            .NotEqual(Guid.Empty)
            .WithError(DomainErrors.StaffTask.AssigneeIdEmpty);
    }
}
