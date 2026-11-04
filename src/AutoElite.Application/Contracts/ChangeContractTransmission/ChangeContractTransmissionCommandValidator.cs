using AutoElite.Application.Common.Validation;
using FluentValidation;

namespace AutoElite.Application.Contracts.ChangeContractTransmission;

/// <summary>Checks that the transmission is defined.</summary>
internal sealed class ChangeContractTransmissionCommandValidator : AbstractValidator<ChangeContractTransmissionCommand>
{
    public ChangeContractTransmissionCommandValidator()
    {
        RuleFor(command => command.Transmission)
            .IsInEnum()
            .WithError(DomainErrors.Contract.TransmissionNotDefined);
    }
}
