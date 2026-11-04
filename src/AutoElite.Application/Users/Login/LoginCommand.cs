using AutoElite.Application.Users.Authentication;

namespace AutoElite.Application.Users.Login;

/// <summary>Signs a user in with email and password and starts a session (FR-1.2).</summary>
/// <param name="Email">The email address the user was invited with.</param>
/// <param name="Password">The password.</param>
[AllowAnonymousRequest]
public sealed record LoginCommand(string? Email, string? Password) : ICommand<AuthTokens>;
