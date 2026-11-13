namespace AutoElite.Client.Authentication;

/// <summary>The tokens of a signed-in session, as the API issued them.</summary>
/// <param name="AccessToken">The access token, sent with every request.</param>
/// <param name="AccessTokenExpiresAt">When the access token expires.</param>
/// <param name="RefreshToken">The refresh token, which renews the session once.</param>
/// <param name="RefreshTokenExpiresAt">When the refresh token expires, and with it the session.</param>
public sealed record AuthSession(
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAt,
    string RefreshToken,
    DateTimeOffset RefreshTokenExpiresAt);
