using System.Net;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Options;
using IPNetwork = System.Net.IPNetwork;

namespace AutoElite.API.Hosting;

/// <summary>
/// The reverse proxies in front of the API whose forwarded headers are trusted, from the optional
/// <see cref="SectionName"/> configuration section. A proxy on the same machine is always trusted; one in a container
/// network or on another machine is named by its address or its network.
/// </summary>
internal sealed class ReverseProxySettings
{
    /// <summary>The configuration section of the settings.</summary>
    public const string SectionName = "ReverseProxy";

    /// <summary>Gets the addresses of the proxies, such as <c>10.0.0.5</c>.</summary>
    public IList<string> KnownProxies { get; } = [];

    /// <summary>Gets the networks of the proxies, such as <c>172.18.0.0/16</c> of a Docker network.</summary>
    public IList<string> KnownNetworks { get; } = [];

    /// <summary>Tells what is wrong with the settings, if anything.</summary>
    /// <returns>The problems, empty when the settings are valid.</returns>
    internal IEnumerable<string> Problems() =>
        KnownProxies
            .Where(proxy => !IPAddress.TryParse(proxy, out _))
            .Select(proxy => $"{SectionName}:KnownProxies has '{proxy}', which is not an IP address.")
            .Concat(KnownNetworks
                .Where(network => !IPNetwork.TryParse(network, out _))
                .Select(network => $"{SectionName}:KnownNetworks has '{network}', which is not a network such as 10.0.0.0/8."));

    /// <summary>Trusts the proxies of the settings in addition to those on the same machine.</summary>
    /// <param name="options">The options of the forwarded headers.</param>
    internal void ApplyTo(ForwardedHeadersOptions options)
    {
        foreach (var proxy in KnownProxies)
            options.KnownProxies.Add(IPAddress.Parse(proxy));

        foreach (var network in KnownNetworks)
            options.KnownIPNetworks.Add(IPNetwork.Parse(network));
    }
}

/// <summary>
/// Reads the client address and the scheme a trusted reverse proxy forwards in X-Forwarded-For and X-Forwarded-Proto,
/// so that the limits of the requests count the clients and not the proxy, and the addresses of created resources
/// have the scheme the client used. The headers of any other sender are ignored, so a client cannot claim another
/// address.
/// </summary>
internal static class ReverseProxy
{
    /// <summary>Adds the options of the forwarded headers with the settings of the configuration.</summary>
    /// <param name="services">The services of the host.</param>
    /// <param name="configuration">The configuration of the host.</param>
    /// <returns>The services, for chaining.</returns>
    public static IServiceCollection AddTrustedProxies(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<ReverseProxySettings>()
            .Bind(configuration.GetSection(ReverseProxySettings.SectionName))
            .ValidateOnStart();
        services.AddSingleton<IValidateOptions<ReverseProxySettings>>(
            new SettingsValidator<ReverseProxySettings>(settings => settings.Problems()));

        services.AddOptions<ForwardedHeadersOptions>()
            .Configure<IOptions<ReverseProxySettings>>((forwarded, settings) =>
            {
                forwarded.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
                settings.Value.ApplyTo(forwarded);
            });

        return services;
    }
}
