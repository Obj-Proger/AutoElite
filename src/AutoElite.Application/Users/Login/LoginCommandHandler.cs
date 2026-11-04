using AutoElite.Application.Users.Authentication;

namespace AutoElite.Application.Users.Login;

/// <summary>
/// Checks the password first and only then the account state, so that only someone who knows the password learns
/// that the account is locked, unconfirmed or deactivated.
/// </summary>
internal sealed class LoginCommandHandler(
    IUserRepository userRepository,
    IIdentityService identityService,
    SessionIssuer sessionIssuer,
    TimeProvider timeProvider) : ICommandHandler<LoginCommand, AuthTokens>
{
    /// <inheritdoc />
    public async Task<Result<AuthTokens>> Handle(LoginCommand command, CancellationToken cancellationToken)
    {
        var emailResult = Email.Create(command.Email);
        var user = emailResult.IsSuccess
            ? await userRepository.GetByEmailAsync(emailResult.Value, cancellationToken)
            : null;

        if (user is null)
            return Result.Failure<AuthTokens>(ApplicationErrors.Account.InvalidCredentials);

        var check = await identityService.CheckPasswordAsync(user.Id, command.Password!, cancellationToken);
        Error? checkError = check switch
        {
            PasswordCheckResult.Succeeded => null,
            PasswordCheckResult.LockedOut => ApplicationErrors.Account.LockedOut,
            PasswordCheckResult.EmailNotConfirmed => ApplicationErrors.Account.EmailNotConfirmed,
            _ => ApplicationErrors.Account.InvalidCredentials
        };

        if (checkError is not null)
            return Result.Failure<AuthTokens>(checkError);

        if (!user.IsActive)
            return Result.Failure<AuthTokens>(ApplicationErrors.Account.UserDeactivated);

        return await sessionIssuer.IssueAsync(user, timeProvider.GetUtcNow().UtcDateTime, cancellationToken);
    }
}
