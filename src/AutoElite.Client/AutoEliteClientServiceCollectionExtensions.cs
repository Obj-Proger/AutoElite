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

        return services.AddAutoEliteClient(() => apiAddress, configureHttpClient);
    }

    /// <summary>
    /// Registers the client of the API for the website, as
    /// <see cref="AddAutoEliteClient(IServiceCollection, Uri, Action{IHttpClientBuilder})"/> does, with
    /// <see cref="BrowserSession"/> as the source of the access tokens: the refresh token stays in the cookie of the API,
    /// and the requests of the session carry the header of the website. The website lets those requests send the cookies
    /// of the browser and registers an <see cref="ISessionRenewalLock"/> across its tabs, if it has one.
    /// </summary>
    /// <param name="services">The services of the website.</param>
    /// <param name="apiAddress">The address of the API.</param>
    /// <param name="configureAuthenticationHttpClient">
    /// Configures the HTTP client of signing in, renewing the session and signing out further, for example so that it
    /// sends the cookies of the browser.
    /// </param>
    /// <param name="configureHttpClient">Configures both HTTP clients further, for example their primary handler.</param>
    /// <returns>The services, for chaining.</returns>
    public static IServiceCollection AddAutoEliteWebsiteClient(
        this IServiceCollection services,
        Uri apiAddress,
        Action<IHttpClientBuilder>? configureAuthenticationHttpClient = null,
        Action<IHttpClientBuilder>? configureHttpClient = null)
    {
        ArgumentNullException.ThrowIfNull(apiAddress);

        services.TryAddSingleton(TimeProvider.System);
        services.AddSingleton(provider => new BrowserSession(
            () => AutoEliteApiClientFactory.Create(
                provider.GetRequiredService<IHttpClientFactory>().CreateClient(AuthenticationHttpClientName)),
            provider.GetRequiredService<TimeProvider>(),
            provider.GetService<ISessionRenewalLock>()));
        services.TryAddSingleton<IAccessTokens>(provider => provider.GetRequiredService<BrowserSession>());
        services.AddAutoEliteClient(apiAddress, configureHttpClient);

        var authentication = services.AddHttpClient(AuthenticationHttpClientName)
            .AddHttpMessageHandler(() => new WebsiteHeaderHandler());
        configureAuthenticationHttpClient?.Invoke(authentication);

        return services;
    }

    /// <summary>
    /// Registers the client of the API, as <see cref="AddAutoEliteClient(IServiceCollection, Uri, Action{IHttpClientBuilder})"/>
    /// does, for an application whose user chooses the API, such as the API of their school: the address is read again
    /// for every client of the API that is created.
    /// </summary>
    /// <param name="services">The services of the application.</param>
    /// <param name="apiAddress">Gives the current address of the API.</param>
    /// <param name="configureHttpClient">Configures both HTTP clients further, for example their primary handler.</param>
    /// <returns>The services, for chaining.</returns>
    public static IServiceCollection AddAutoEliteClient(
        this IServiceCollection services, Func<Uri> apiAddress, Action<IHttpClientBuilder>? configureHttpClient = null)
    {
        ArgumentNullException.ThrowIfNull(apiAddress);

        services.TryAddSingleton<IAuthSessionStore, InMemoryAuthSessionStore>();
        services.TryAddSingleton(TimeProvider.System);

        var authentication = services.AddHttpClient(AuthenticationHttpClientName, http => http.BaseAddress = apiAddress());
        var api = services.AddHttpClient(ApiHttpClientName, http => http.BaseAddress = apiAddress())
            .AddHttpMessageHandler<AuthenticationHandler>();
        configureHttpClient?.Invoke(authentication);
        configureHttpClient?.Invoke(api);

        services.AddSingleton(provider => new AuthSessionManager(
            () => AutoEliteApiClientFactory.Create(
                provider.GetRequiredService<IHttpClientFactory>().CreateClient(AuthenticationHttpClientName)),
            provider.GetRequiredService<IAuthSessionStore>(),
            provider.GetRequiredService<TimeProvider>()));

        // The access tokens come from the session above unless the application registered its own source first, as the
        // website does, whose refresh token stays in a cookie.
        services.TryAddSingleton<IAccessTokens>(provider => provider.GetRequiredService<AuthSessionManager>());
        services.AddTransient<AuthenticationHandler>();
        services.AddTransient(provider => AutoEliteApiClientFactory.Create(
            provider.GetRequiredService<IHttpClientFactory>().CreateClient(ApiHttpClientName)));

        return services;
    }
}
