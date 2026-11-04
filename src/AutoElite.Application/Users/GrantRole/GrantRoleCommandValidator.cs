using AutoElite.Application.Common.Validation;
using FluentValidation;

namespace AutoElite.Application.Users.GrantRole;

/// <summary>Checks that the role is defined.</summary>
internal sealed class GrantRoleCommandValidator : AbstractValidator<GrantRoleCommand>
{
    public GrantRoleCommandValidator()
    {
        RuleFor(command => command.Role)
            .IsInEnum()
            .WithError(DomainErrors.UserRoleAssignment.RoleNotDefined);
    }
}
