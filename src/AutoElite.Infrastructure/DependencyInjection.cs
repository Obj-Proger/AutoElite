using AutoElite.Application.Branches;
using AutoElite.Application.Settings;
using AutoElite.Application.Users;
using AutoElite.Application.Users.Authentication;
using AutoElite.Infrastructure.Authentication;
using AutoElite.Infrastructure.Identity;
using AutoElite.Infrastructure.Persistence;
using AutoElite.Infrastructure.Persistence.Interceptors;
using AutoElite.Infrastructure.Persistence.Queries;
using AutoElite.Infrastructure.Persistence.Repositories;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace AutoElite.Infrastructure;

/// <summary>Registers the infrastructure layer in the dependency injection container.</summary>
public static class DependencyInjection
{
    /// <summary>The name of the connection string of the school's database.</summary>
    public const string ConnectionStringName = "AutoElite";

    /// <summary>
    /// Registers the database context, the unit of work, the sign-in accounts and session tokens, the authentication of
    /// requests, and the repositories and read services implemented so far. The host must register
    /// <see cref="ICurrentUser"/> and logging, and configure the <see cref="JwtOptions.SectionName"/> and
    /// <see cref="ClientOptions.SectionName"/> sections.
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
        AddSessions(services, configuration);

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

    /// <summary>
    /// Registers the session tokens and the links to the web client, with their settings checked when the host starts,
    /// and the authentication of requests by access token, with the same rules the tokens are created with.
    /// </summary>
    private static void AddSessions(IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<JwtOptions>().Bind(configuration.GetSection(JwtOptions.SectionName)).ValidateOnStart();
        services.AddSingleton<IValidateOptions<JwtOptions>>(new SettingsValidator<JwtOptions>(options => options.Problems()));
        services.AddOptions<ClientOptions>().Bind(configuration.GetSection(ClientOptions.SectionName)).ValidateOnStart();
        services.AddSingleton<IValidateOptions<ClientOptions>>(
            new SettingsValidator<ClientOptions>(options => options.Problems()));

        services.AddSingleton<IAccessTokenFactory, JwtAccessTokenFactory>();
        services.AddScoped<IRefreshTokenStore, RefreshTokenStore>();
        services.AddSingleton<IClientLinks, ClientLinks>();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<JwtOptions>>((bearer, jwt) =>
            {
                // Claims keep the names the factory writes, such as "sub" and "role".
                bearer.MapInboundClaims = false;
                bearer.TokenValidationParameters = JwtAccessTokenFactory.ValidationParameters(jwt.Value);
            });
    }

    /// <summary>The number of wrong passwords in a row after which an account is locked.</summary>
    internal const int MaxFailedSignIns = 5;

    /// <summary>How long a locked account stays locked.</summary>
    internal static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);

    /// <summary>How long a password reset or email change link is valid.</summary>
    internal static readonly TimeSpan EmailLinkLifetime = TimeSpan.FromHours(2);
}
