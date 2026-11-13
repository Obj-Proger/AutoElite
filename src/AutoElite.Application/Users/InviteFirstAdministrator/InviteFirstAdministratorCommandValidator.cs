using AutoElite.Application.Common.Validation;
using FluentValidation;

namespace AutoElite.Application.Users.InviteFirstAdministrator;

/// <summary>Checks the details of the first administrator.</summary>
internal sealed class InviteFirstAdministratorCommandValidator : AbstractValidator<InviteFirstAdministratorCommand>
{
    public InviteFirstAdministratorCommandValidator()
    {
        RuleFor(command => command.LastName).ValidLastName();
        RuleFor(command => command.FirstName).ValidFirstName();
        RuleFor(command => command.Email).ValidEmail();

        RuleFor(command => command.PreferredLanguage)
            .IsInEnum()
            .WithError(DomainErrors.User.LanguageNotDefined);
    }
}
