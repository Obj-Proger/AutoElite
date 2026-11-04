using AutoElite.Application.Common.Validation;
using FluentValidation;

namespace AutoElite.Application.Users.RefreshTokens;

/// <summary>Checks that a refresh token is given.</summary>
internal sealed class RefreshTokensCommandValidator : AbstractValidator<RefreshTokensCommand>
{
    public RefreshTokensCommandValidator()
    {
        RuleFor(command => command.RefreshToken)
            .NotEmpty()
            .WithError(ApplicationErrors.Account.RefreshTokenInvalid);
    }
}
