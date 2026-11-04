using AutoElite.Application.Common.Validation;
using FluentValidation;

namespace AutoElite.Application.Leads.SubmitLeadRequest;

/// <summary>
/// Checks the optional phone, email, category and branch. The contact name and the rule that a phone or an email is
/// required are checked by the domain.
/// </summary>
internal sealed class SubmitLeadRequestCommandValidator : AbstractValidator<SubmitLeadRequestCommand>
{
    public SubmitLeadRequestCommandValidator()
    {
        When(command => !string.IsNullOrWhiteSpace(command.Phone), () => RuleFor(command => command.Phone).ValidPhone());
        When(command => !string.IsNullOrWhiteSpace(command.Email), () => RuleFor(command => command.Email).ValidEmail());

        RuleFor(command => command.DesiredCategory)
            .IsInEnum()
            .WithError(DomainErrors.Lead.CategoryNotDefined);

        RuleFor(command => command.BranchId)
            .NotEqual(Guid.Empty)
            .WithError(DomainErrors.Lead.BranchIdEmpty);
    }
}
