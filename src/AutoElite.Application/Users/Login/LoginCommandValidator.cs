using AutoElite.Application.Common.Validation;
using FluentValidation;

namespace AutoElite.Application.Users.Login;

/// <summary>
/// Checks only that both fields are filled in. The email format is not checked, so a mistyped address gets the
/// same answer as a wrong password.
/// </summary>
internal sealed class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(command => command.Email)
            .NotEmpty()
            .WithError(DomainErrors.Email.Empty);

        RuleFor(command => command.Password)
            .NotEmpty()
            .WithError(ApplicationErrors.Password.Empty);
    }
}
