using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using AutoElite.API.Common;

namespace AutoElite.API.Tests;

[Collection(nameof(ApiCollection))]
public sealed class HostTests(ApiFactory api)
{
    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    [Theory]
    [InlineData(HealthEndpoints.Live)]
    [InlineData(HealthEndpoints.Ready)]
    public async Task Health_WithTheDatabaseUp_IsHealthy(string endpoint)
    {
        using var client = api.CreateClient();

        using var response = await client.GetAsync(endpoint, CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await response.Content.ReadAsStringAsync(CancellationToken)).Should().Be("Healthy");
    }

    [Fact]
    public async Task OpenApiDocument_IsPublished()
    {
        using var client = api.CreateClient();

        using var document = await client.GetFromJsonAsync<JsonDocument>("/openapi/v1.json", CancellationToken);

        document!.RootElement.GetProperty("openapi").GetString().Should().StartWith("3.");
    }

    [Fact]
    public async Task UnknownAddress_IsAnsweredWithProblemDetails()
    {
        using var client = api.CreateClient();

        using var response = await client.GetAsync("/api/v1/no-such-thing", CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
        using var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync(CancellationToken));
        problem.RootElement.GetProperty("status").GetInt32().Should().Be(404);
    }
}
