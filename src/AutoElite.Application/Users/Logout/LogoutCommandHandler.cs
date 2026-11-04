using AutoElite.Application.Users.Authentication;

namespace AutoElite.Application.Users.Logout;

/// <summary>Revokes the refresh token, if one is given.</summary>
internal sealed class LogoutCommandHandler(IRefreshTokenStore refreshTokenStore, TimeProvider timeProvider)
    : ICommandHandler<LogoutCommand>
{
    /// <inheritdoc />
    public async Task<Result> Handle(LogoutCommand command, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrEmpty(command.RefreshToken))
            await refreshTokenStore.RevokeAsync(command.RefreshToken, timeProvider.GetUtcNow().UtcDateTime, cancellationToken);

        return Result.Success();
    }
}
