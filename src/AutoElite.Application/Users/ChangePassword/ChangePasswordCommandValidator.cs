using AutoElite.Application.Common.Validation;
using FluentValidation;

namespace AutoElite.Application.Users.ChangePassword;

/// <summary>Checks that the current password is given and the new one follows the policy and differs from it.</summary>
internal sealed class ChangePasswordCommandValidator : AbstractValidator<ChangePasswordCommand>
{
    public ChangePasswordCommandValidator()
    {
        RuleFor(command => command.CurrentPassword)
            .NotEmpty()
            .WithError(ApplicationErrors.Password.Empty);

        RuleFor(command => command.NewPassword).ValidPassword();

        RuleFor(command => command.NewPassword)
            .Must((command, newPassword) => string.IsNullOrEmpty(newPassword) || newPassword != command.CurrentPassword)
            .WithError(ApplicationErrors.Password.SameAsCurrent);
    }
}
