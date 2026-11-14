namespace AutoElite.API.DemoData;

/// <summary>Registers the demo data of development.</summary>
internal static class DemoDataServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="DemoDataSeeder"/>, which creates the demo data when the host starts, when the environment is
    /// Development and <c>DemoData:Enabled</c> is set; in any other environment nothing is registered.
    /// </summary>
    /// <param name="services">The services of the host.</param>
    /// <param name="configuration">The configuration of the host.</param>
    /// <param name="environment">The environment of the host.</param>
    /// <returns>The services, for chaining.</returns>
    public static IServiceCollection AddDemoData(
        this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        var section = configuration.GetSection(DemoDataOptions.SectionName);
        if (!environment.IsDevelopment() || !section.GetValue<bool>(nameof(DemoDataOptions.Enabled)))
            return services;

        services.AddOptions<DemoDataOptions>().Bind(section);
        services.AddHostedService<DemoDataSeeder>();

        return services;
    }
}
