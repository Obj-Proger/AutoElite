using Microsoft.AspNetCore.Components.WebAssembly.Http;

namespace AutoElite.Web.Authentication;

/// <summary>
/// Lets the requests of signing in, renewing the session and signing out send and receive the cookie of the session,
/// which the API, at another origin than the site, sets for its endpoints of the browser session only.
/// </summary>
internal sealed class BrowserCookiesHandler : DelegatingHandler
{
    /// <inheritdoc />
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        request.SetBrowserRequestCredentials(BrowserRequestCredentials.Include);

        return base.SendAsync(request, cancellationToken);
    }
}
