using AutoElite.Application.Common.Validation;
using FluentValidation;

namespace AutoElite.Application.Users.RequestEmailChange;

/// <summary>Checks the new address and that the current password is given.</summary>
internal sealed class RequestEmailChangeCommandValidator : AbstractValidator<RequestEmailChangeCommand>
{
    public RequestEmailChangeCommandValidator()
    {
        RuleFor(command => command.NewEmail).ValidEmail();

        RuleFor(command => command.CurrentPassword)
            .NotEmpty()
            .WithError(ApplicationErrors.Password.Empty);
    }
}
