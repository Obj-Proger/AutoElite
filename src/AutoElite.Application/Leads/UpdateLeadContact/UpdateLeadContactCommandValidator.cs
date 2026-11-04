using AutoElite.Application.Common.Validation;
using FluentValidation;

namespace AutoElite.Application.Leads.UpdateLeadContact;

/// <summary>Checks the optional contacts and the category.</summary>
internal sealed class UpdateLeadContactCommandValidator : AbstractValidator<UpdateLeadContactCommand>
{
    public UpdateLeadContactCommandValidator()
    {
        When(command => !string.IsNullOrWhiteSpace(command.Phone), () => RuleFor(command => command.Phone).ValidPhone());
        When(command => !string.IsNullOrWhiteSpace(command.Email), () => RuleFor(command => command.Email).ValidEmail());

        RuleFor(command => command.DesiredCategory)
            .IsInEnum()
            .WithError(DomainErrors.Lead.CategoryNotDefined);
    }
}
