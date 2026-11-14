using AutoElite.Client.Api;
using AutoElite.Client.Authentication;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace AutoElite.Client.Offline;

/// <summary>Registers the offline mode of an application on a device.</summary>
public static class OfflineServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="OfflineDatabase"/> on the file, <see cref="OfflineCache"/>, <see cref="OfflineQueue"/> and
    /// <see cref="OfflineSynchronizer"/>, which sends the queue with the <see cref="AutoEliteApiClient"/> that
    /// <c>AddAutoEliteClient</c> registers. The data is encrypted under a key in the <see cref="ISecretStore"/> of the
    /// platform, which the application registers.
    /// </summary>
    /// <param name="services">The services of the application.</param>
    /// <param name="databasePath">The file of the offline database, in the private storage of the application.</param>
    /// <returns>The services, for chaining.</returns>
    public static IServiceCollection AddAutoEliteOffline(this IServiceCollection services, string databasePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databasePath);

        services.TryAddSingleton(TimeProvider.System);
        services.AddSingleton(provider => new OfflineDatabase(databasePath, provider.GetRequiredService<ISecretStore>()));
        services.AddSingleton<OfflineCache>();
        services.AddSingleton<OfflineQueue>();
        services.AddSingleton(provider => new OfflineSynchronizer(
            provider.GetRequiredService<OfflineQueue>(), () => provider.GetRequiredService<AutoEliteApiClient>()));

        return services;
    }
}
