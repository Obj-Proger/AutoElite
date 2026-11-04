namespace AutoElite.Application.Users.Authentication;

/// <summary>A token given to a client, with the moment it stops being accepted.</summary>
/// <param name="Value">The token as the client sends it back.</param>
/// <param name="ExpiresAtUtc">The UTC time after which the token is rejected.</param>
public sealed record IssuedToken(string Value, DateTime ExpiresAtUtc);
