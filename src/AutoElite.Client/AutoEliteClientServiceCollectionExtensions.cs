using AutoElite.Client.Api;
using AutoElite.Client.Authentication;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace AutoElite.Client;

/// <summary>Registers the client of the API with the session of the signed-in user.</summary>
public static class AutoEliteClientServiceCollectionExtensions
{
    /// <summary>The name of the HTTP client of the requests that carry the access token.</summary>
    public const string ApiHttpClientName = "AutoElite.Api";

    /// <summary>The name of the HTTP client of signing in, renewing the session and signing out.</summary>
    public const string AuthenticationHttpClientName = "AutoElite.Authentication";

    /// <summary>
    /// Registers <see cref="AutoEliteApiClient"/>, whose requests carry the access token of the session;
    /// <see cref="AuthSessionManager"/>; and, unless the application registers its own, a store that keeps the session
    /// in memory only.
    /// </summary>
    /// <param name="services">The services of the application.</param>
    /// <param name="apiAddress">The address of the API.</param>
    /// <param name="configureHttpClient">Configures both HTTP clients further, for example their primary handler.</param>
    /// <returns>The services, for chaining.</returns>
    public static IServiceCollection AddAutoEliteClient(
        this IServiceCollection services, Uri apiAddress, Action<IHttpClientBuilder>? configureHttpClient = null)
    {
        ArgumentNullException.ThrowIfNull(apiAddress);

        services.TryAddSingleton<IAuthSessionStore, InMemoryAuthSessionStore>();
        services.TryAddSingleton(TimeProvider.System);

        var authentication = services.AddHttpClient(AuthenticationHttpClientName, http => http.BaseAddress = apiAddress);
        var api = services.AddHttpClient(ApiHttpClientName, http => http.BaseAddress = apiAddress)
            .AddHttpMessageHandler<AuthenticationHandler>();
        configureHttpClient?.Invoke(authentication);
        configureHttpClient?.Invoke(api);

        services.AddSingleton(provider => new AuthSessionManager(
            () => AutoEliteApiClientFactory.Create(
                provider.GetRequiredService<IHttpClientFactory>().CreateClient(AuthenticationHttpClientName)),
            provider.GetRequiredService<IAuthSessionStore>(),
            provider.GetRequiredService<TimeProvider>()));
        services.AddTransient<AuthenticationHandler>();
        services.AddTransient(provider => AutoEliteApiClientFactory.Create(
            provider.GetRequiredService<IHttpClientFactory>().CreateClient(ApiHttpClientName)));

        return services;
    }
}
