using AutoElite.Client.Api;
using Microsoft.Kiota.Abstractions.Authentication;
using Microsoft.Kiota.Bundle;

namespace AutoElite.Client;

/// <summary>
/// Creates the generated client of the API (<see cref="AutoEliteApiClient"/>) over an HTTP client whose base address is
/// the address of the API. The HTTP client carries the access token: its handlers add it to the requests.
/// </summary>
public static class AutoEliteApiClientFactory
{
    /// <summary>Creates the client of the API.</summary>
    /// <param name="httpClient">The HTTP client, with the address of the API as its base address; the caller owns it.</param>
    /// <returns>The client of the API.</returns>
    public static AutoEliteApiClient Create(HttpClient httpClient)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        if (httpClient.BaseAddress is null)
            throw new ArgumentException("The HTTP client needs the address of the API as its base address.", nameof(httpClient));

        var adapter = new DefaultRequestAdapter(new AnonymousAuthenticationProvider(), httpClient: httpClient)
        {
            BaseUrl = httpClient.BaseAddress.GetLeftPart(UriPartial.Path).TrimEnd('/')
        };

        return new AutoEliteApiClient(adapter);
    }
}
