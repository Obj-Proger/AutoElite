namespace AutoElite.Infrastructure.Authentication;

/// <summary>
/// The address of the web client, from the <see cref="SectionName"/> configuration section, for the links in emails.
/// </summary>
public sealed class ClientOptions
{
    /// <summary>The configuration section of the settings.</summary>
    public const string SectionName = "Client";

    /// <summary>
    /// Gets or sets the absolute address the web client is served from, for example <c>https://school.example.ru/</c>.
    /// </summary>
    public Uri? BaseUrl { get; set; }

    /// <summary>Tells what is wrong with the settings, if anything.</summary>
    /// <returns>The problems, empty when the settings are valid.</returns>
    internal IEnumerable<string> Problems()
    {
        var isWebAddress = BaseUrl is { IsAbsoluteUri: true } url
            && (url.Scheme == Uri.UriSchemeHttps || url.Scheme == Uri.UriSchemeHttp);
        if (!isWebAddress)
            yield return $"{SectionName}:BaseUrl must be the absolute http or https address of the web client.";
    }
}
