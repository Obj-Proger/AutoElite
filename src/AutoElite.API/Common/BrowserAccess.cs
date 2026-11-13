using AutoElite.Application.Users.Authentication;

namespace AutoElite.API.Common;

/// <summary>
/// The access token of a session of the website. Its refresh token is not in the answer: it is kept in an HttpOnly
/// cookie, which the scripts of the page cannot read.
/// </summary>
/// <param name="AccessToken">The access token, which the website keeps in memory only.</param>
public sealed record BrowserAccess(IssuedToken AccessToken);
