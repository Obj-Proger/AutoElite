using System.Net.Http.Json;

namespace AutoElite.API.Tests.Hosting;

/// <summary>The headers that tell browsers how to treat the responses of the API.</summary>
[Collection(nameof(ApiCollection))]
public sealed class SecurityHeadersTests(ApiFactory api)
{
    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    private static void ShouldHaveTheHeadersOfEveryResponse(HttpResponseMessage response)
    {
        response.Headers.GetValues("X-Content-Type-Options").Should().Equal("nosniff");
        response.Headers.GetValues("X-Frame-Options").Should().Equal("DENY");
        response.Headers.GetValues("Content-Security-Policy").Should().Equal("frame-ancestors 'none'");
        response.Headers.GetValues("Referrer-Policy").Should().Equal("no-referrer");
    }

    [Fact]
    public async Task EveryResponse_TellsBrowsersHowToTreatIt()
    {
        using var client = api.CreateClient();

        using var probe = await client.GetAsync(new Uri("/health/live", UriKind.Relative), CancellationToken);
        using var refused = await client.GetAsync(new Uri("/api/v1/courses", UriKind.Relative), CancellationToken);
        using var dashboard = await client.GetAsync(new Uri("/jobs", UriKind.Relative), CancellationToken);

        ShouldHaveTheHeadersOfEveryResponse(probe);
        ShouldHaveTheHeadersOfEveryResponse(refused);
        dashboard.Headers.GetValues("X-Frame-Options").Should().Contain("DENY");
        dashboard.Headers.GetValues("Content-Security-Policy").Should().Contain("frame-ancestors 'none'");
        refused.Headers.CacheControl?.NoStore.Should().NotBe(true);
    }

    [Fact]
    public async Task AnswersOfAuthentication_AreNeverStored()
    {
        using var client = api.CreateClient();

        using var response = await client.PostAsJsonAsync(
            "/api/v1/auth/login", new { Email = "nobody@example.com", Password = "Not-the-password-1" }, CancellationToken);

        ShouldHaveTheHeadersOfEveryResponse(response);
        response.Headers.CacheControl!.NoStore.Should().BeTrue();
        response.Headers.Pragma.Should().ContainSingle(pragma => pragma.Name == "no-cache");
    }
}
