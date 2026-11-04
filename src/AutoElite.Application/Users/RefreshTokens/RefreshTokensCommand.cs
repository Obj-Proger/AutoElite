using AutoElite.Application.Users.Authentication;

namespace AutoElite.Application.Users.RefreshTokens;

/// <summary>
/// Exchanges a refresh token for new tokens (FR-1.3). The presented token is used up; the roles in the new access
/// token are read again, so role changes take effect at the next renewal.
/// </summary>
/// <param name="RefreshToken">The refresh token issued at sign-in or at the last renewal.</param>
[AllowAnonymousRequest]
public sealed record RefreshTokensCommand(string? RefreshToken) : ICommand<AuthTokens>;
