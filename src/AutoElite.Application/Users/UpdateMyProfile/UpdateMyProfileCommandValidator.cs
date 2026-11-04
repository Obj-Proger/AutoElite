using AutoElite.Application.Common.Validation;
using FluentValidation;

namespace AutoElite.Application.Users.UpdateMyProfile;

/// <summary>Checks the new profile details.</summary>
internal sealed class UpdateMyProfileCommandValidator : AbstractValidator<UpdateMyProfileCommand>
{
    public UpdateMyProfileCommandValidator()
    {
        RuleFor(command => command.LastName).ValidLastName();
        RuleFor(command => command.FirstName).ValidFirstName();
        RuleFor(command => command.MiddleName).ValidMiddleName();

        When(command => !string.IsNullOrWhiteSpace(command.Phone), () => RuleFor(command => command.Phone).ValidPhone());

        RuleFor(command => command.PreferredLanguage)
            .IsInEnum()
            .WithError(DomainErrors.User.LanguageNotDefined);
    }
}
