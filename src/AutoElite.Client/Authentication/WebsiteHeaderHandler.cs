namespace AutoElite.Client.Authentication;

/// <summary>
/// Adds the header of the website to the requests of signing in, renewing the session and signing out, without which
/// the endpoints of the browser session refuse them: a page of another site cannot send it without a CORS preflight,
/// which the API refuses.
/// </summary>
public sealed class WebsiteHeaderHandler : DelegatingHandler
{
    /// <summary>The name of the header.</summary>
    public const string HeaderName = "X-AutoElite-Client";

    /// <summary>The value of the header.</summary>
    public const string HeaderValue = "browser";

    /// <inheritdoc />
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        request.Headers.Remove(HeaderName);
        request.Headers.Add(HeaderName, HeaderValue);

        return base.SendAsync(request, cancellationToken);
    }
}
