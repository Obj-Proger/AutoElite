using AutoElite.Application.Common.Validation;
using FluentValidation;

namespace AutoElite.Application.Users.ResetPassword;

/// <summary>Checks the reset link parameters and the new password.</summary>
internal sealed class ResetPasswordCommandValidator : AbstractValidator<ResetPasswordCommand>
{
    public ResetPasswordCommandValidator()
    {
        RuleFor(command => command.UserId)
            .NotEmpty()
            .WithError(ApplicationErrors.Account.PasswordResetInvalid);

        RuleFor(command => command.Token)
            .NotEmpty()
            .WithError(ApplicationErrors.Account.PasswordResetInvalid);

        RuleFor(command => command.NewPassword).ValidPassword();
    }
}
