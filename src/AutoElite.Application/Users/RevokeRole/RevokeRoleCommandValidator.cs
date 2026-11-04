using AutoElite.Application.Common.Validation;
using FluentValidation;

namespace AutoElite.Application.Users.RevokeRole;

/// <summary>Checks that the role is defined.</summary>
internal sealed class RevokeRoleCommandValidator : AbstractValidator<RevokeRoleCommand>
{
    public RevokeRoleCommandValidator()
    {
        RuleFor(command => command.Role)
            .IsInEnum()
            .WithError(DomainErrors.UserRoleAssignment.RoleNotDefined);
    }
}
