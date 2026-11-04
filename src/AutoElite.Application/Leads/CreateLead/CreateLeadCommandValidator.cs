using AutoElite.Application.Common.Validation;
using FluentValidation;

namespace AutoElite.Application.Leads.CreateLead;

/// <summary>Checks the optional contacts, the source, the category and the identifiers.</summary>
internal sealed class CreateLeadCommandValidator : AbstractValidator<CreateLeadCommand>
{
    public CreateLeadCommandValidator()
    {
        When(command => !string.IsNullOrWhiteSpace(command.Phone), () => RuleFor(command => command.Phone).ValidPhone());
        When(command => !string.IsNullOrWhiteSpace(command.Email), () => RuleFor(command => command.Email).ValidEmail());

        RuleFor(command => command.Source)
            .IsInEnum()
            .WithError(DomainErrors.Lead.SourceNotDefined);

        RuleFor(command => command.DesiredCategory)
            .IsInEnum()
            .WithError(DomainErrors.Lead.CategoryNotDefined);

        RuleFor(command => command.BranchId)
            .NotEqual(Guid.Empty)
            .WithError(DomainErrors.Lead.BranchIdEmpty);

        RuleFor(command => command.ManagerId)
            .NotEqual(Guid.Empty)
            .WithError(DomainErrors.Lead.ManagerIdEmpty);
    }
}
