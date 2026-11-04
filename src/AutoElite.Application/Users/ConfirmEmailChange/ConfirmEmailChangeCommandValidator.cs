using AutoElite.Application.Common.Validation;
using FluentValidation;

namespace AutoElite.Application.Users.ConfirmEmailChange;

/// <summary>Checks that the link carries all its parameters.</summary>
internal sealed class ConfirmEmailChangeCommandValidator : AbstractValidator<ConfirmEmailChangeCommand>
{
    public ConfirmEmailChangeCommandValidator()
    {
        RuleFor(command => command.UserId)
            .NotEmpty()
            .WithError(ApplicationErrors.Account.EmailChangeInvalid);

        RuleFor(command => command.NewEmail)
            .NotEmpty()
            .WithError(ApplicationErrors.Account.EmailChangeInvalid);

        RuleFor(command => command.Token)
            .NotEmpty()
            .WithError(ApplicationErrors.Account.EmailChangeInvalid);
    }
}
