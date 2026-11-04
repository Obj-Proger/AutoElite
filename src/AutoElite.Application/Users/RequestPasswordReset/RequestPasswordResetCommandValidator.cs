using AutoElite.Application.Common.Validation;
using FluentValidation;

namespace AutoElite.Application.Users.RequestPasswordReset;

/// <summary>Checks only that the address is filled in; a malformed one is answered like an unknown one.</summary>
internal sealed class RequestPasswordResetCommandValidator : AbstractValidator<RequestPasswordResetCommand>
{
    public RequestPasswordResetCommandValidator()
    {
        RuleFor(command => command.Email)
            .NotEmpty()
            .WithError(DomainErrors.Email.Empty);
    }
}
