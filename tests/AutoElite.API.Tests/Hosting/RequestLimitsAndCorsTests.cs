using System.Net;

namespace AutoElite.API.Tests.Hosting;

/// <summary>The limits of the requests a client sends and the origins whose pages may call the API.</summary>
[Collection(nameof(ApiCollection))]
public sealed class RequestLimitsAndCorsTests(ApiFactory api)
{
    private const string WebClientOrigin = "http://localhost:5173";

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    private static HttpRequestMessage Preflight(string origin)
    {
        var request = new HttpRequestMessage(HttpMethod.Options, "/api/v1/courses");
        request.Headers.Add("Origin", origin);
        request.Headers.Add("Access-Control-Request-Method", "GET");
        request.Headers.Add("Access-Control-Request-Headers", "authorization");

        return request;
    }

    [Fact]
    public async Task SensitiveEndpoint_OverItsLimit_AnswersTooManyRequests()
    {
        await using var limited = api.WithWebHostBuilder(
            builder => builder.UseSetting("RateLimiting:SensitivePermitsPerMinute", "1"));
        using var client = limited.CreateClient();
        var credentials = new { Email = $"{Guid.NewGuid():N}@example.com", Password = "Not-the-password-1" };

        using var first = await client.SendJsonAsync(HttpMethod.Post, "/api/v1/auth/login", credentials);
        using var second = await client.SendJsonAsync(HttpMethod.Post, "/api/v1/auth/login", credentials);

        first.StatusCode.Should().NotBe(HttpStatusCode.TooManyRequests);
        second.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        second.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
        second.Headers.RetryAfter!.Delta.Should().BePositive();
        var problem = await second.ReadJsonAsync();
        problem.GetProperty("status").GetInt32().Should().Be(429);
        problem.GetProperty("code").GetString().Should().Be("Request.TooManyRequests");
    }

    [Fact]
    public async Task WebClient_IsAllowedToCallTheApiFromTheBrowser()
    {
        using var client = api.CreateClient();
        using var request = Preflight(WebClientOrigin);

        using var response = await client.SendAsync(request, CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        response.Headers.GetValues("Access-Control-Allow-Origin").Should().Equal(WebClientOrigin);
        response.Headers.Contains("Access-Control-Allow-Credentials").Should().BeFalse();
    }

    [Fact]
    public async Task AnotherSite_IsNotAllowedToCallTheApiFromTheBrowser()
    {
        using var client = api.CreateClient();
        using var request = Preflight("https://another-site.example");

        using var response = await client.SendAsync(request, CancellationToken);

        response.Headers.Contains("Access-Control-Allow-Origin").Should().BeFalse();
    }
}
