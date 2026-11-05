using AutoElite.Application.Branches;
using AutoElite.Application.Settings;
using AutoElite.Application.Users;
using AutoElite.Infrastructure.Identity;
using AutoElite.Infrastructure.Persistence;
using AutoElite.Infrastructure.Persistence.Interceptors;
using AutoElite.Infrastructure.Persistence.Queries;
using AutoElite.Infrastructure.Persistence.Repositories;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AutoElite.Infrastructure;

/// <summary>Registers the infrastructure layer in the dependency injection container.</summary>
public static class DependencyInjection
{
    /// <summary>The name of the connection string of the school's database.</summary>
    public const string ConnectionStringName = "AutoElite";

    /// <summary>
    /// Registers the database context, the unit of work, the sign-in accounts, and the repositories and read services
    /// implemented so far. The host must register <see cref="ICurrentUser"/> and logging.
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
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IUserRoleRepository, UserRoleRepository>();
        services.AddScoped<IUserQueries, UserQueries>();

        AddAccounts(services);

        return services;
    }

    /// <summary>
    /// Registers ASP.NET Core Identity for the sign-in accounts, with the password rules of <see cref="PasswordPolicy"/>,
    /// a lockout after repeated wrong passwords, and data protection keys kept in the database.
    /// </summary>
    private static void AddAccounts(IServiceCollection services)
    {
        services.AddDataProtection()
            .SetApplicationName("AutoElite")
            .PersistKeysToDbContext<AutoEliteDbContext>();

        services.AddIdentityCore<UserAccount>(options =>
            {
                options.Password.RequiredLength = PasswordPolicy.MinLength;
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = false;
                options.Password.RequireUppercase = false;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequiredUniqueChars = 1;

                options.Lockout.AllowedForNewUsers = true;
                options.Lockout.MaxFailedAccessAttempts = MaxFailedSignIns;
                options.Lockout.DefaultLockoutTimeSpan = LockoutDuration;

                // The sign-in name is the email address, so any character an address may contain is allowed.
                options.User.AllowedUserNameCharacters = string.Empty;
            })
            .AddUserStore<AccountStore>()
            .AddDefaultTokenProviders()
            .AddTokenProvider<InvitationTokenProvider>(InvitationTokenProvider.ProviderName);

        // Password reset and email change links: short-lived, since the user is waiting for them.
        services.Configure<DataProtectionTokenProviderOptions>(options => options.TokenLifespan = EmailLinkLifetime);
        services.AddScoped<IIdentityService, IdentityService>();
    }

    /// <summary>The number of wrong passwords in a row after which an account is locked.</summary>
    internal const int MaxFailedSignIns = 5;

    /// <summary>How long a locked account stays locked.</summary>
    internal static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);

    /// <summary>How long a password reset or email change link is valid.</summary>
    internal static readonly TimeSpan EmailLinkLifetime = TimeSpan.FromHours(2);
}
