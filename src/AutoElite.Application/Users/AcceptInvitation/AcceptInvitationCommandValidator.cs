using AutoElite.Application.Common.Validation;
using FluentValidation;

namespace AutoElite.Application.Users.AcceptInvitation;

/// <summary>Checks the invitation link parameters and the new password.</summary>
internal sealed class AcceptInvitationCommandValidator : AbstractValidator<AcceptInvitationCommand>
{
    public AcceptInvitationCommandValidator()
    {
        RuleFor(command => command.UserId)
            .NotEmpty()
            .WithError(ApplicationErrors.Account.InvitationInvalid);

        RuleFor(command => command.Token)
            .NotEmpty()
            .WithError(ApplicationErrors.Account.InvitationInvalid);

        RuleFor(command => command.Password).ValidPassword();
    }
}
