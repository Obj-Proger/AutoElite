using AutoElite.Application.Common.Validation;
using FluentValidation;

namespace AutoElite.Application.Payments.RegisterPayment;

/// <summary>Checks the method; the amount, purpose and reference are checked by the domain.</summary>
internal sealed class RegisterPaymentCommandValidator : AbstractValidator<RegisterPaymentCommand>
{
    public RegisterPaymentCommandValidator()
    {
        RuleFor(command => command.Method)
            .IsInEnum()
            .WithError(DomainErrors.Payment.MethodNotDefined);
    }
}
