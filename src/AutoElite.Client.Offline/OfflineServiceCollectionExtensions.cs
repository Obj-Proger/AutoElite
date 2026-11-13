using AutoElite.Client.Api;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace AutoElite.Client.Offline;

/// <summary>Registers the offline mode of an application on a device.</summary>
public static class OfflineServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="OfflineDatabase"/> on the file, <see cref="OfflineCache"/>, <see cref="OfflineQueue"/> and
    /// <see cref="OfflineSynchronizer"/>, which sends the queue with the <see cref="AutoEliteApiClient"/> that
    /// <c>AddAutoEliteClient</c> registers.
    /// </summary>
    /// <param name="services">The services of the application.</param>
    /// <param name="databasePath">The file of the offline database, in the private storage of the application.</param>
    /// <returns>The services, for chaining.</returns>
    public static IServiceCollection AddAutoEliteOffline(this IServiceCollection services, string databasePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databasePath);

        services.TryAddSingleton(TimeProvider.System);
        services.AddSingleton(_ => new OfflineDatabase(databasePath));
        services.AddSingleton<OfflineCache>();
        services.AddSingleton<OfflineQueue>();
        services.AddSingleton(provider => new OfflineSynchronizer(
            provider.GetRequiredService<OfflineQueue>(), () => provider.GetRequiredService<AutoEliteApiClient>()));

        return services;
    }
}
