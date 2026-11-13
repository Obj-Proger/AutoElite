using System.Net;
using System.Net.Http.Headers;

namespace AutoElite.Client.Authentication;

/// <summary>
/// Sends the access token of the session with each request to the API and, when the API rejects it, renews the session
/// and sends the request once more. The content of a request is buffered first, so that it can be sent again.
/// </summary>
/// <param name="sessions">The session of the signed-in user.</param>
public sealed class AuthenticationHandler(AuthSessionManager sessions) : DelegatingHandler
{
    /// <inheritdoc />
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (await sessions.GetAccessTokenAsync(cancellationToken) is not { } accessToken)
            return await base.SendAsync(request, cancellationToken);

        if (request.Content is not null)
            await request.Content.LoadIntoBufferAsync(cancellationToken);

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        var response = await base.SendAsync(request, cancellationToken);
        if (response.StatusCode != HttpStatusCode.Unauthorized
            || await sessions.RenewAsync(accessToken, cancellationToken) is not { } renewedAccessToken)
        {
            return response;
        }

        response.Dispose();
        var repeated = Repeat(request);
        repeated.Headers.Authorization = new AuthenticationHeaderValue("Bearer", renewedAccessToken);

        return await base.SendAsync(repeated, cancellationToken);
    }

    // A message can be sent only once; the repeated one shares the buffered content, which the caller disposes with
    // the original message.
    private static HttpRequestMessage Repeat(HttpRequestMessage request)
    {
        var repeated = new HttpRequestMessage(request.Method, request.RequestUri)
        {
            Content = request.Content,
            Version = request.Version,
            VersionPolicy = request.VersionPolicy
        };
        foreach (var header in request.Headers)
            repeated.Headers.TryAddWithoutValidation(header.Key, header.Value);

        foreach (var option in request.Options)
            ((IDictionary<string, object?>)repeated.Options)[option.Key] = option.Value;

        return repeated;
    }
}
