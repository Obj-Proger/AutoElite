using AutoElite.Application.Common.Validation;
using FluentValidation;

namespace AutoElite.Application.Leads.ChangeLeadBranch;

/// <summary>Checks that the branch identifier, if given, is not empty.</summary>
internal sealed class ChangeLeadBranchCommandValidator : AbstractValidator<ChangeLeadBranchCommand>
{
    public ChangeLeadBranchCommandValidator()
    {
        RuleFor(command => command.BranchId)
            .NotEqual(Guid.Empty)
            .WithError(DomainErrors.Lead.BranchIdEmpty);
    }
}
