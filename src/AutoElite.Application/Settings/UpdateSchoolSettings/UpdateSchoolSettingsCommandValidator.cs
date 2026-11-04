using AutoElite.Application.Common.Validation;
using FluentValidation;

namespace AutoElite.Application.Settings.UpdateSchoolSettings;

/// <summary>Checks the currency; the time zone and the ranges of the other values are the domain's rules.</summary>
internal sealed class UpdateSchoolSettingsCommandValidator : AbstractValidator<UpdateSchoolSettingsCommand>
{
    public UpdateSchoolSettingsCommandValidator()
    {
        RuleFor(command => command.DefaultCurrency).ValidatedBy(Currency.Create);
    }
}
