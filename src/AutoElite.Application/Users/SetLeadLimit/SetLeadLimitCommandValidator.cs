using AutoElite.Application.Common.Validation;
using FluentValidation;

namespace AutoElite.Application.Users.SetLeadLimit;

/// <summary>Checks that the limit is from 1 to <see cref="User.MaxLeadLimit"/>.</summary>
internal sealed class SetLeadLimitCommandValidator : AbstractValidator<SetLeadLimitCommand>
{
    public SetLeadLimitCommandValidator()
    {
        RuleFor(command => command.LeadLimit)
            .InclusiveBetween(1, User.MaxLeadLimit)
            .WithError(DomainErrors.User.LeadLimitOutOfRange);
    }
}
