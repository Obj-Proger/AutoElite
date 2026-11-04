namespace AutoElite.Application.Users.Logout;

/// <summary>
/// Ends a session by revoking its refresh token. It is open to anonymous callers, because the access token may
/// already have expired; an unknown token is ignored, so signing out always succeeds.
/// </summary>
/// <param name="RefreshToken">The refresh token of the session.</param>
[AllowAnonymousRequest]
public sealed record LogoutCommand(string? RefreshToken) : ICommand;
