using AutoElite.Application.Branches;
using AutoElite.Application.Settings;
using AutoElite.Infrastructure.Persistence;
using AutoElite.Infrastructure.Persistence.Interceptors;
using AutoElite.Infrastructure.Persistence.Queries;
using AutoElite.Infrastructure.Persistence.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AutoElite.Infrastructure;

/// <summary>Registers the infrastructure layer in the dependency injection container.</summary>
public static class DependencyInjection
{
    /// <summary>The name of the connection string of the school's database.</summary>
    public const string ConnectionStringName = "AutoElite";

    /// <summary>
    /// Registers the database context, the unit of work, and the repositories and read services implemented so far.
    /// The host must register <see cref="ICurrentUser"/>.
    /// </summary>
    /// <param name="services">The services to add to.</param>
    /// <param name="configuration">The configuration with the connection string <see cref="ConnectionStringName"/>.</param>
    /// <returns>The same services.</returns>
    /// <exception cref="InvalidOperationException">The connection string is missing.</exception>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString(ConnectionStringName);
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                $"The connection string \"{ConnectionStringName}\" is missing. Set ConnectionStrings:{ConnectionStringName} "
                + $"with dotnet user-secrets locally or the ConnectionStrings__{ConnectionStringName} environment variable when deployed.");
        }

        services.AddScoped<AuditInterceptor>();
        services.AddDbContext<AutoEliteDbContext>((provider, options) =>
            options.UseAutoElite(connectionString, provider.GetRequiredService<AuditInterceptor>()));
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        services.AddScoped<IBranchRepository, BranchRepository>();
        services.AddScoped<IBranchQueries, BranchQueries>();
        services.AddScoped<ISchoolSettingsRepository, SchoolSettingsRepository>();

        return services;
    }
}
