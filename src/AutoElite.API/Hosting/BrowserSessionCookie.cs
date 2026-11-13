using AutoElite.API.Common;
using AutoElite.Application.Users.Authentication;

namespace AutoElite.API.Hosting;

/// <summary>
/// The cookie that holds the refresh token of a session of the website, so that no script of the page can read it:
/// HttpOnly, Secure, SameSite=Strict, sent only to the endpoints of the browser session and expiring with the token.
/// Those endpoints also require a header of the website, which a page of another site cannot send without a CORS
/// preflight that the API refuses, so that no other site can renew or end a session.
/// </summary>
internal static class BrowserSessionCookie
{
    /// <summary>The name of the cookie; the prefix makes browsers accept it only as Secure.</summary>
    public const string Name = "__Secure-AutoElite-Refresh";

    /// <summary>The header the website sends with each request to the endpoints of the browser session.</summary>
    public const string ClientHeader = "X-AutoElite-Client";

    /// <summary>The value of <see cref="ClientHeader"/>.</summary>
    public const string ClientHeaderValue = "browser";

    /// <summary>The path of the endpoints of the browser session, the only one the cookie is sent to.</summary>
    public const string Path = "/" + ApiRoutes.Prefix + "/auth/browser";

    /// <summary>Tells whether a request comes from the website, by its header.</summary>
    /// <param name="request">The request.</param>
    /// <returns><see langword="true"/> when the request carries the header of the website.</returns>
    public static bool IsFromWebsite(HttpRequest request) =>
        string.Equals(request.Headers[ClientHeader], ClientHeaderValue, StringComparison.Ordinal);

    /// <summary>Reads the refresh token of the request.</summary>
    /// <param name="request">The request.</param>
    /// <returns>The refresh token, or <see langword="null"/> when the request has none.</returns>
    public static string? Read(HttpRequest request) =>
        request.Cookies.TryGetValue(Name, out var value) && !string.IsNullOrEmpty(value) ? value : null;

    /// <summary>Sets the refresh token of a new or renewed session.</summary>
    /// <param name="response">The response.</param>
    /// <param name="refreshToken">The refresh token.</param>
    public static void Write(HttpResponse response, IssuedToken refreshToken) =>
        response.Cookies.Append(
            Name,
            refreshToken.Value,
            Options(new DateTimeOffset(DateTime.SpecifyKind(refreshToken.ExpiresAtUtc, DateTimeKind.Utc))));

    /// <summary>Removes the refresh token, when the session ends or cannot be renewed.</summary>
    /// <param name="response">The response.</param>
    public static void Delete(HttpResponse response) => response.Cookies.Delete(Name, Options(expires: null));

    private static CookieOptions Options(DateTimeOffset? expires) => new()
    {
        HttpOnly = true,
        Secure = true,
        SameSite = SameSiteMode.Strict,
        Path = Path,
        Expires = expires,
        IsEssential = true
    };
}
