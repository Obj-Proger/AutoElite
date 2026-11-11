using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace AutoElite.API.Tests;

/// <summary>The calls the tests make to the API, reading the JSON of the answers as a client reads it.</summary>
internal static class ApiCalls
{
    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    /// <summary>Reads the JSON of an answer.</summary>
    /// <param name="response">The answer.</param>
    public static async Task<JsonElement> ReadJsonAsync(this HttpResponseMessage response) =>
        await response.Content.ReadFromJsonAsync<JsonElement>(ApiJson.Options, CancellationToken);

    /// <summary>Reads a resource, which must be found.</summary>
    /// <param name="client">The client.</param>
    /// <param name="uri">The address of the resource.</param>
    public static async Task<JsonElement> GetJsonAsync(this HttpClient client, string uri)
    {
        using var response = await client.GetAsync(uri, CancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        return await response.ReadJsonAsync();
    }

    /// <summary>Creates a resource, which must be created, and gives its identifier.</summary>
    /// <param name="client">The client.</param>
    /// <param name="uri">The address of the collection.</param>
    /// <param name="body">The resource.</param>
    public static async Task<Guid> CreateAsync(this HttpClient client, string uri, object body)
    {
        using var response = await client.PostAsJsonAsync(uri, body, ApiJson.Options, CancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        return (await response.ReadJsonAsync()).GetProperty("id").GetGuid();
    }

    /// <summary>Sends JSON, enumerations as their names, and gives the answer.</summary>
    /// <param name="client">The client.</param>
    /// <param name="method">The method.</param>
    /// <param name="uri">The address.</param>
    /// <param name="body">The body, or <see langword="null"/> for none.</param>
    public static async Task<HttpResponseMessage> SendJsonAsync(this HttpClient client, HttpMethod method, string uri, object? body = null)
    {
        using var request = new HttpRequestMessage(method, uri)
        {
            Content = body is null ? null : JsonContent.Create(body, options: ApiJson.Options)
        };

        return await client.SendAsync(request, CancellationToken);
    }
}
