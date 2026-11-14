using System.Net;
using AutoElite.Client;
using AutoElite.Client.Api;
using AutoElite.Client.Authentication;
using AutoElite.Domain.Enums;
using Microsoft.AspNetCore.Mvc.Testing.Handlers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Kiota.Abstractions;

namespace AutoElite.API.Tests.Client;

/// <summary>
/// The session of the website in the client of the API: the refresh token in the cookie of the API, which the browser
/// keeps, and the access token in the memory of the page. A page is a service provider; the pages of one browser share
/// its cookies.
/// </summary>
[Collection(nameof(ApiCollection))]
public sealed class WebsiteSessionTests(ApiFactory api)
{
    // Over HTTPS, as in production: the browser keeps the cookie of the session only over HTTPS.
    private static readonly Uri _apiAddress = new("https://localhost/");
    private static readonly Uri _sessionAddress = new("https://localhost/api/v1/auth/browser/refresh");

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    // A page of the website in a browser whose cookies the container keeps.
    private ServiceProvider Page(CookieContainer browser, ISessionRenewalLock? renewalLock = null)
    {
        var services = new ServiceCollection();
        if (renewalLock is not null)
            services.AddSingleton(renewalLock);

        return services
            .AddAutoEliteWebsiteClient(
                _apiAddress,
                configureHttpClient: http => http.ConfigurePrimaryHttpMessageHandler(
                    () => new CookieContainerHandler(browser) { InnerHandler = api.Server.CreateHandler() }))
            .BuildServiceProvider();
    }

    private static BrowserSession SessionOf(ServiceProvider page) => page.GetRequiredService<BrowserSession>();

    private static async Task<string?> ReadOwnEmailAsync(ServiceProvider page) =>
        (await page.GetRequiredService<AutoEliteApiClient>().Api.V1.Me.GetAsync(cancellationToken: CancellationToken))?.Email;

    private static string? CookieOf(CookieContainer browser) =>
        browser.GetCookies(_sessionAddress).SingleOrDefault(cookie => cookie.Name == "__Secure-AutoElite-Refresh")?.Value;

    private async Task<InvitedUser> StudentAsync()
    {
        var (client, user) = await api.SignInAsync(UserRole.Student);
        client.Dispose();

        return user;
    }

    [Fact]
    public async Task SignedInWebsite_ReadsTheProfile_AndAnotherPageRestoresTheSession()
    {
        var user = await StudentAsync();
        var browser = new CookieContainer();
        await using var first = Page(browser);
        await SessionOf(first).SignInAsync(user.Email, ApiFactory.Password, CancellationToken);

        await using var second = Page(browser);
        var restored = await SessionOf(second).RestoreAsync(CancellationToken);

        (await ReadOwnEmailAsync(first)).Should().Be(user.Email);
        restored.Should().BeTrue();
        (await ReadOwnEmailAsync(second)).Should().Be(user.Email);
    }

    [Fact]
    public async Task PageWithoutTheCookie_IsNotSignedIn()
    {
        await using var page = Page(new CookieContainer());
        var session = SessionOf(page);

        var restored = await session.RestoreAsync(CancellationToken);
        var accessToken = await session.GetAccessTokenAsync(CancellationToken);
        var read = () => ReadOwnEmailAsync(page);

        restored.Should().BeFalse();
        accessToken.Should().BeNull();
        (await read.Should().ThrowAsync<ApiException>()).Which.ResponseStatusCode.Should().Be(401);
    }

    [Fact]
    public async Task WrongPassword_IsRefused_AndLeavesNoCookie()
    {
        var user = await StudentAsync();
        var browser = new CookieContainer();
        await using var page = Page(browser);

        var signIn = () => SessionOf(page).SignInAsync(user.Email, "Not-the-password-1", CancellationToken);

        (await signIn.Should().ThrowAsync<ApiException>()).Which.ResponseStatusCode.Should().BeOneOf(400, 401);
        SessionOf(page).IsSignedIn.Should().BeFalse();
        CookieOf(browser).Should().BeNull();
    }

    [Fact]
    public async Task Renewal_GivesANewAccessToken_AndReplacesTheCookie()
    {
        var user = await StudentAsync();
        var browser = new CookieContainer();
        await using var page = Page(browser);
        var session = SessionOf(page);
        await session.SignInAsync(user.Email, ApiFactory.Password, CancellationToken);
        var accessToken = await session.GetAccessTokenAsync(CancellationToken);
        var cookie = CookieOf(browser);

        var renewed = await session.RenewAsync(accessToken!, CancellationToken);

        renewed.Should().NotBeNullOrEmpty().And.NotBe(accessToken);
        CookieOf(browser).Should().NotBeNullOrEmpty().And.NotBe(cookie);
        (await ReadOwnEmailAsync(page)).Should().Be(user.Email);
    }

    [Fact]
    public async Task TabsThatRenewAtOnce_TakeTurns_AndKeepTheSession()
    {
        var user = await StudentAsync();
        var browser = new CookieContainer();
        using var renewalLock = new SharedLock();
        await using var first = Page(browser, renewalLock);
        await using var second = Page(browser, renewalLock);
        await SessionOf(first).SignInAsync(user.Email, ApiFactory.Password, CancellationToken);
        await SessionOf(second).RestoreAsync(CancellationToken);
        var firstToken = await SessionOf(first).GetAccessTokenAsync(CancellationToken);
        var secondToken = await SessionOf(second).GetAccessTokenAsync(CancellationToken);

        var renewed = await Task.WhenAll(
            SessionOf(first).RenewAsync(firstToken!, CancellationToken),
            SessionOf(second).RenewAsync(secondToken!, CancellationToken));

        renewed.Should().AllSatisfy(token => token.Should().NotBeNullOrEmpty());
        renewalLock.MostAtOnce.Should().Be(1);
        (await ReadOwnEmailAsync(first)).Should().Be(user.Email);
        (await ReadOwnEmailAsync(second)).Should().Be(user.Email);
    }

    [Fact]
    public async Task SignOut_EndsTheSession_AndAnotherPageLearnsItWhenItRenews()
    {
        var user = await StudentAsync();
        var browser = new CookieContainer();
        await using var first = Page(browser);
        await using var second = Page(browser);
        await SessionOf(first).SignInAsync(user.Email, ApiFactory.Password, CancellationToken);
        await SessionOf(second).RestoreAsync(CancellationToken);
        var secondToken = await SessionOf(second).GetAccessTokenAsync(CancellationToken);
        var ended = 0;
        SessionOf(second).Changed += (_, _) => ended++;

        await SessionOf(first).SignOutAsync(CancellationToken);
        var renewed = await SessionOf(second).RenewAsync(secondToken!, CancellationToken);

        SessionOf(first).IsSignedIn.Should().BeFalse();
        CookieOf(browser).Should().BeNull();
        renewed.Should().BeNull();
        SessionOf(second).IsSignedIn.Should().BeFalse();
        ended.Should().Be(1);
        await using var third = Page(browser);
        (await SessionOf(third).RestoreAsync(CancellationToken)).Should().BeFalse();
    }

    // A lock that every page of the test shares, as the Web Locks API is shared by the tabs of a browser, which counts
    // how many renewals ran at once.
    private sealed class SharedLock : ISessionRenewalLock, IDisposable
    {
        private readonly SemaphoreSlim _semaphore = new(1, 1);
        private int _running;

        public int MostAtOnce { get; private set; }

        public async Task RunAsync(Func<Task> renewal, CancellationToken cancellationToken)
        {
            await _semaphore.WaitAsync(cancellationToken);
            try
            {
                MostAtOnce = Math.Max(MostAtOnce, Interlocked.Increment(ref _running));
                await renewal();
            }
            finally
            {
                Interlocked.Decrement(ref _running);
                _semaphore.Release();
            }
        }

        public void Dispose() => _semaphore.Dispose();
    }
}
