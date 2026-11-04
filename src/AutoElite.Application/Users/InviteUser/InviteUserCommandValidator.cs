using AutoElite.Application.Common.Validation;
using FluentValidation;

namespace AutoElite.Application.Users.InviteUser;

/// <summary>Checks the details of a new user.</summary>
internal sealed class InviteUserCommandValidator : AbstractValidator<InviteUserCommand>
{
    public InviteUserCommandValidator()
    {
        RuleFor(command => command.LastName).ValidLastName();
        RuleFor(command => command.FirstName).ValidFirstName();
        RuleFor(command => command.MiddleName).ValidMiddleName();
        RuleFor(command => command.Email).ValidEmail();

        When(command => !string.IsNullOrWhiteSpace(command.Phone), () => RuleFor(command => command.Phone).ValidPhone());

        RuleFor(command => command.BranchId)
            .NotEqual(Guid.Empty)
            .WithError(DomainErrors.User.BranchIdEmpty);

        RuleFor(command => command.Roles)
            .NotEmpty()
            .WithError(ApplicationErrors.Account.RolesEmpty);

        RuleFor(command => command.Roles)
            .Must(roles => roles is null || roles.Distinct().Count() == roles.Count)
            .WithError(ApplicationErrors.Account.DuplicateRoles);

        RuleForEach(command => command.Roles!)
            .IsInEnum()
            .WithError(DomainErrors.UserRoleAssignment.RoleNotDefined);

        RuleFor(command => command.PreferredLanguage)
            .IsInEnum()
            .WithError(DomainErrors.User.LanguageNotDefined);
    }
}
