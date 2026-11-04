using AutoElite.Application.Common.Validation;
using FluentValidation;

namespace AutoElite.Application.Users.ChangeUserBranch;

/// <summary>Checks that the branch identifier, if given, is not empty.</summary>
internal sealed class ChangeUserBranchCommandValidator : AbstractValidator<ChangeUserBranchCommand>
{
    public ChangeUserBranchCommandValidator()
    {
        RuleFor(command => command.BranchId)
            .NotEqual(Guid.Empty)
            .WithError(DomainErrors.User.BranchIdEmpty);
    }
}
