using AutoElite.Application.Users.Authentication;

namespace AutoElite.Application.Users.ResetPassword;

/// <summary>
/// Replaces the password through the identity system. A wrong or expired link, an unknown user and a deactivated user
/// get the same error.
/// </summary>
internal sealed class ResetPasswordCommandHandler(
    IUserRepository userRepository,
    IIdentityService identityService,
    IRefreshTokenStore refreshTokenStore,
    TimeProvider timeProvider) : ICommandHandler<ResetPasswordCommand>
{
    /// <inheritdoc />
    public async Task<Result> Handle(ResetPasswordCommand command, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(command.UserId, cancellationToken);
        if (user is not { IsActive: true })
            return Result.Failure(ApplicationErrors.Account.PasswordResetInvalid);

        var reset = await identityService.ResetPasswordAsync(user.Id, command.Token!, command.NewPassword!, cancellationToken);
        if (!reset)
            return Result.Failure(ApplicationErrors.Account.PasswordResetInvalid);

        await refreshTokenStore.RevokeAllAsync(user.Id, timeProvider.GetUtcNow().UtcDateTime, cancellationToken);

        return Result.Success();
    }
}
