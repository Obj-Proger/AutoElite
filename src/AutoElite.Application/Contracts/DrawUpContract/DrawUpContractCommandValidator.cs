using AutoElite.Application.Common.Validation;
using FluentValidation;

namespace AutoElite.Application.Contracts.DrawUpContract;

/// <summary>Checks the transmission and the branch identifier; the other terms are checked by the domain.</summary>
internal sealed class DrawUpContractCommandValidator : AbstractValidator<DrawUpContractCommand>
{
    public DrawUpContractCommandValidator()
    {
        RuleFor(command => command.Transmission)
            .IsInEnum()
            .WithError(DomainErrors.Contract.TransmissionNotDefined);

        RuleFor(command => command.BranchId)
            .NotEqual(Guid.Empty)
            .WithError(DomainErrors.Contract.BranchIdEmpty);
    }
}
