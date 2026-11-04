using AutoElite.Application.Users.Authentication;

namespace AutoElite.Application.Users.ChangePassword;

/// <summary>Replaces the password, ends every session of the user and starts a new one.</summary>
internal sealed class ChangePasswordCommandHandler(
    IUserRepository userRepository,
    IIdentityService identityService,
    IRefreshTokenStore refreshTokenStore,
    SessionIssuer sessionIssuer,
    ICurrentUser currentUser,
    TimeProvider timeProvider) : ICommandHandler<ChangePasswordCommand, AuthTokens>
{
    /// <inheritdoc />
    public async Task<Result<AuthTokens>> Handle(ChangePasswordCommand command, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(currentUser.GetRequiredUserId(), cancellationToken);
        if (user is null)
            return Result.Failure<AuthTokens>(ApplicationErrors.NotFound<User>());

        if (!user.IsActive)
            return Result.Failure<AuthTokens>(ApplicationErrors.Account.UserDeactivated);

        var changed = await identityService.ChangePasswordAsync(
            user.Id, command.CurrentPassword!, command.NewPassword!, cancellationToken);
        if (!changed)
            return Result.Failure<AuthTokens>(ApplicationErrors.Account.CurrentPasswordWrong);

        var utcNow = timeProvider.GetUtcNow().UtcDateTime;
        await refreshTokenStore.RevokeAllAsync(user.Id, utcNow, cancellationToken);

        return await sessionIssuer.IssueAsync(user, utcNow, cancellationToken);
    }
}
