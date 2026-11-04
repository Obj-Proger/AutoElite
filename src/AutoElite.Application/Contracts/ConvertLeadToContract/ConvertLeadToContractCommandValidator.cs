using AutoElite.Application.Common.Validation;
using FluentValidation;

namespace AutoElite.Application.Contracts.ConvertLeadToContract;

/// <summary>
/// Checks the details of a new student and the transmission. The email may come from the lead, so it is checked
/// here only when given.
/// </summary>
internal sealed class ConvertLeadToContractCommandValidator : AbstractValidator<ConvertLeadToContractCommand>
{
    public ConvertLeadToContractCommandValidator()
    {
        RuleFor(command => command.ExistingStudentId)
            .NotEqual(Guid.Empty)
            .WithError(DomainErrors.Lead.UserIdEmpty);

        When(command => command.ExistingStudentId is null, () =>
        {
            RuleFor(command => command.LastName).ValidLastName();
            RuleFor(command => command.FirstName).ValidFirstName();
            RuleFor(command => command.MiddleName).ValidMiddleName();

            When(command => !string.IsNullOrWhiteSpace(command.Email), () => RuleFor(command => command.Email).ValidEmail());
            When(command => !string.IsNullOrWhiteSpace(command.Phone), () => RuleFor(command => command.Phone).ValidPhone());

            RuleFor(command => command.PreferredLanguage)
                .IsInEnum()
                .WithError(DomainErrors.User.LanguageNotDefined);
        });

        RuleFor(command => command.Transmission)
            .IsInEnum()
            .WithError(DomainErrors.Contract.TransmissionNotDefined);
    }
}
