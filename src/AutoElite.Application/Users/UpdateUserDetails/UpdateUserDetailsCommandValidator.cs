using AutoElite.Application.Common.Validation;
using FluentValidation;

namespace AutoElite.Application.Users.UpdateUserDetails;

/// <summary>Checks the new details.</summary>
internal sealed class UpdateUserDetailsCommandValidator : AbstractValidator<UpdateUserDetailsCommand>
{
    public UpdateUserDetailsCommandValidator()
    {
        RuleFor(command => command.LastName).ValidLastName();
        RuleFor(command => command.FirstName).ValidFirstName();
        RuleFor(command => command.MiddleName).ValidMiddleName();

        When(command => !string.IsNullOrWhiteSpace(command.Phone), () => RuleFor(command => command.Phone).ValidPhone());
    }
}
