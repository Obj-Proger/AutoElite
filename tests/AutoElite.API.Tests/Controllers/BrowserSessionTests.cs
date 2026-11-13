using System.Net;
using System.Net.Http.Json;
using AutoElite.Domain.Enums;
using Microsoft.AspNetCore.Mvc.Testing;

namespace AutoElite.API.Tests.Controllers;

/// <summary>
/// The session of the website: its refresh token lives in an HttpOnly cookie that no script reads, and its endpoints
/// answer only requests with the header of the website.
/// </summary>
[Collection(nameof(ApiCollection))]
public sealed class BrowserSessionTests(ApiFactory api)
{
    private const string CookieName = "__Secure-AutoElite-Refresh";

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    // A browser of the website: it sends the header of the website and, as a test, its cookie by hand.
    private HttpClient Website()
    {
        var client = api.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });
        client.DefaultRequestHeaders.Add("X-AutoElite-Client", "browser");

        return client;
    }

    private static string? SetCookie(HttpResponseMessage response) =>
        response.Headers.TryGetValues("Set-Cookie", out var cookies)
            ? cookies.SingleOrDefault(cookie => cookie.StartsWith(CookieName + "=", StringComparison.Ordinal))
            : null;

    // The name and value of a Set-Cookie header, as the browser sends the cookie back.
    private static string CookieOf(string setCookie) => setCookie.Split(';')[0];

    private static HttpRequestMessage WithCookie(string uri, string cookie) =>
        new(HttpMethod.Post, uri) { Headers = { { "Cookie", cookie } } };

    private async Task<(HttpResponseMessage Response, string Cookie)> SignInAsync(HttpClient website)
    {
        var (client, user) = await api.SignInAsync(UserRole.Student);
        client.Dispose();
        var response = await website.PostAsJsonAsync(
            "/api/v1/auth/browser/login", new { user.Email, Password = ApiFactory.Password }, CancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        return (response, CookieOf(SetCookie(response)!));
    }

    [Fact]
    public async Task Login_KeepsTheRefreshTokenInAnHttpOnlyCookie_AndAnswersTheAccessTokenOnly()
    {
        using var website = Website();

        var (response, _) = await SignInAsync(website);
        using var login = response;
        var body = await login.ReadJsonAsync();
        var setCookie = SetCookie(login)!.ToUpperInvariant();

        body.GetProperty("accessToken").GetProperty("value").GetString().Should().NotBeNullOrEmpty();
        body.TryGetProperty("refreshToken", out _).Should().BeFalse();
        setCookie.Should().Contain("HTTPONLY").And.Contain("SECURE").And.Contain("SAMESITE=STRICT")
            .And.Contain("PATH=/API/V1/AUTH/BROWSER").And.Contain("EXPIRES=");
    }

    [Fact]
    public async Task Refresh_ReplacesTheCookie_AndTheReplacedOneNoLongerWorks()
    {
        using var website = Website();
        var (login, cookie) = await SignInAsync(website);
        login.Dispose();

        using var refreshRequest = WithCookie("/api/v1/auth/browser/refresh", cookie);
        using var refreshed = await website.SendAsync(refreshRequest, CancellationToken);
        var renewedCookie = SetCookie(refreshed);
        using var reuseRequest = WithCookie("/api/v1/auth/browser/refresh", cookie);
        using var reused = await website.SendAsync(reuseRequest, CancellationToken);

        refreshed.StatusCode.Should().Be(HttpStatusCode.OK);
        (await refreshed.ReadJsonAsync()).GetProperty("accessToken").GetProperty("value").GetString().Should().NotBeNullOrEmpty();
        renewedCookie.Should().NotBeNull();
        CookieOf(renewedCookie!).Should().NotBe(cookie);
        reused.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        SetCookie(reused).Should().NotBeNull("the cookie of a session that cannot be renewed is removed");
    }

    [Fact]
    public async Task Logout_EndsTheSession_AndRemovesTheCookie()
    {
        using var website = Website();
        var (login, cookie) = await SignInAsync(website);
        login.Dispose();

        using var logoutRequest = WithCookie("/api/v1/auth/browser/logout", cookie);
        using var logout = await website.SendAsync(logoutRequest, CancellationToken);
        using var refreshRequest = WithCookie("/api/v1/auth/browser/refresh", cookie);
        using var refresh = await website.SendAsync(refreshRequest, CancellationToken);

        logout.StatusCode.Should().Be(HttpStatusCode.NoContent);
        SetCookie(logout).Should().NotBeNull().And.Contain("expires=Thu, 01 Jan 1970");
        refresh.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Refresh_WithoutACookie_AnswersUnauthorized()
    {
        using var website = Website();

        using var response = await website.PostAsync(new Uri("/api/v1/auth/browser/refresh", UriKind.Relative), null, CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Requests_WithoutTheHeaderOfTheWebsite_AreRefused()
    {
        using var website = Website();
        var (login, cookie) = await SignInAsync(website);
        login.Dispose();
        using var other = api.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });

        using var request = WithCookie("/api/v1/auth/browser/refresh", cookie);
        using var response = await other.SendAsync(request, CancellationToken);
        using var loginWithoutHeader = await other.PostAsJsonAsync(
            "/api/v1/auth/browser/login", new { Email = "nobody@example.com", Password = "Not-the-password-1" }, CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        loginWithoutHeader.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        SetCookie(response).Should().BeNull();
    }
}
