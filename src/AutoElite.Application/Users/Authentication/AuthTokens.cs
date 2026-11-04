namespace AutoElite.Application.Users.Authentication;

/// <summary>The tokens of a session: a short-lived access token for requests and a refresh token to renew it.</summary>
/// <param name="AccessToken">The access token, sent with every request.</param>
/// <param name="RefreshToken">The single-use refresh token, exchanged for new tokens when the access token expires.</param>
public sealed record AuthTokens(IssuedToken AccessToken, IssuedToken RefreshToken);
