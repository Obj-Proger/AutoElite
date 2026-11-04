using AutoElite.Application.Users.Authentication;

namespace AutoElite.Application.Users.RefreshTokens;

/// <summary>Redeems the refresh token and issues new tokens to a user who is still active.</summary>
internal sealed class RefreshTokensCommandHandler(
    IUserRepository userRepository,
    IRefreshTokenStore refreshTokenStore,
    SessionIssuer sessionIssuer,
    TimeProvider timeProvider) : ICommandHandler<RefreshTokensCommand, AuthTokens>
{
    /// <inheritdoc />
    public async Task<Result<AuthTokens>> Handle(RefreshTokensCommand command, CancellationToken cancellationToken)
    {
        var utcNow = timeProvider.GetUtcNow().UtcDateTime;

        var userId = await refreshTokenStore.RedeemAsync(command.RefreshToken!, utcNow, cancellationToken);
        var user = userId is { } id ? await userRepository.GetByIdAsync(id, cancellationToken) : null;

        if (user is not { IsActive: true })
            return Result.Failure<AuthTokens>(ApplicationErrors.Account.RefreshTokenInvalid);

        return await sessionIssuer.IssueAsync(user, utcNow, cancellationToken);
    }
}
