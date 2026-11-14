using System.Globalization;
using System.Net.Http.Headers;

namespace AutoElite.Client;

/// <summary>
/// Asks the API to answer in the language of the user interface, so that its errors, such as a field that failed
/// validation, and the texts it writes are in the language the user reads; a request that already asks for a language,
/// such as one of a browser, keeps it.
/// </summary>
public sealed class AcceptLanguageHandler : DelegatingHandler
{
    /// <inheritdoc />
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var culture = CultureInfo.CurrentUICulture;
        if (request.Headers.AcceptLanguage.Count == 0 && !string.IsNullOrEmpty(culture.Name))
            request.Headers.AcceptLanguage.Add(new StringWithQualityHeaderValue(culture.Name));

        return base.SendAsync(request, cancellationToken);
    }
}
