namespace AutoElite.Application.Users.Authentication;

/// <summary>Issues the tokens of a new or renewed session.</summary>
internal sealed class SessionIssuer(
    IUserRoleRepository userRoleRepository,
    IAccessTokenFactory accessTokenFactory,
    IRefreshTokenStore refreshTokenStore)
{
    /// <summary>Creates an access token with the user's current roles and a new refresh token.</summary>
    /// <param name="user">The signed-in user.</param>
    /// <param name="utcNow">The current UTC time.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    public async Task<AuthTokens> IssueAsync(User user, DateTime utcNow, CancellationToken cancellationToken)
    {
        var roles = await userRoleRepository.GetRolesAsync(user.Id, cancellationToken);
        var accessToken = accessTokenFactory.Create(user, roles, utcNow);
        var refreshToken = await refreshTokenStore.IssueAsync(user.Id, utcNow, cancellationToken);

        return new AuthTokens(accessToken, refreshToken);
    }
}
