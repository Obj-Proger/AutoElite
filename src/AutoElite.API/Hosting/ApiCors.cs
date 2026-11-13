using AutoElite.Infrastructure.Authentication;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.Extensions.Options;

namespace AutoElite.API.Hosting;

/// <summary>
/// The web addresses besides the web client whose pages may call the API, from the optional <see cref="SectionName"/>
/// configuration section, for example a web client served from another port while it is developed.
/// </summary>
internal sealed class CorsSettings
{
    /// <summary>The configuration section of the settings.</summary>
    public const string SectionName = "Cors";

    /// <summary>Gets the origins, each a scheme, a host and a port only, such as <c>https://admin.example.ru</c>.</summary>
    public IList<string> AllowedOrigins { get; } = [];

    /// <summary>Tells what is wrong with the settings, if anything.</summary>
    /// <returns>The problems, empty when the settings are valid.</returns>
    internal IEnumerable<string> Problems() =>
        AllowedOrigins
            .Where(origin => OriginOf(origin) is null)
            .Select(origin => $"{SectionName}:AllowedOrigins has '{origin}', which is not an http or https origin without a path.");

    /// <summary>Gets an origin as browsers send it, or <see langword="null"/> if the text is not one.</summary>
    /// <param name="origin">The text of the origin.</param>
    internal static string? OriginOf(string origin) =>
        Uri.TryCreate(origin, UriKind.Absolute, out var url)
        && (url.Scheme == Uri.UriSchemeHttps || url.Scheme == Uri.UriSchemeHttp)
        && url.AbsolutePath == "/" && url.Query.Length == 0 && url.Fragment.Length == 0
            ? url.GetLeftPart(UriPartial.Authority)
            : null;
}

/// <summary>
/// Lets the pages of the web client, at <see cref="ClientOptions.BaseUrl"/>, and of the origins of
/// <see cref="CorsSettings"/> call the API from the browser. They send the access token in the Authorization header;
/// credentials are allowed for these origins only, so that the website can send the cookie of its session to the
/// endpoints of the browser session (<see cref="BrowserSessionCookie"/>). The headers that tell where a created
/// resource is, the name of a download and when to try again are exposed to them.
/// </summary>
internal static class ApiCors
{
    // How long a browser may keep the answer to a preflight request.
    private static readonly TimeSpan _preflightMaxAge = TimeSpan.FromMinutes(10);

    /// <summary>Adds the policy with the settings of the configuration.</summary>
    /// <param name="services">The services of the host.</param>
    /// <param name="configuration">The configuration of the host.</param>
    /// <returns>The services, for chaining.</returns>
    public static IServiceCollection AddApiCors(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<CorsSettings>()
            .Bind(configuration.GetSection(CorsSettings.SectionName))
            .ValidateOnStart();
        services.AddSingleton<IValidateOptions<CorsSettings>>(new SettingsValidator<CorsSettings>(settings => settings.Problems()));

        services.AddCors();
        services.AddOptions<CorsOptions>()
            .Configure<IOptions<ClientOptions>, IOptions<CorsSettings>>((cors, client, settings) =>
                cors.AddDefaultPolicy(policy => policy
                    .WithOrigins([.. OriginsOf(client.Value, settings.Value)])
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials()
                    .WithExposedHeaders("Location", "Content-Disposition", "Retry-After")
                    .SetPreflightMaxAge(_preflightMaxAge)));

        return services;
    }

    private static IEnumerable<string> OriginsOf(ClientOptions client, CorsSettings settings)
    {
        if (client.BaseUrl is { IsAbsoluteUri: true } baseUrl)
            yield return baseUrl.GetLeftPart(UriPartial.Authority);

        foreach (var origin in settings.AllowedOrigins)
        {
            if (CorsSettings.OriginOf(origin) is { } valid)
                yield return valid;
        }
    }
}
