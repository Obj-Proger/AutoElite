using System.Globalization;
using System.Security.Claims;
using System.Threading.RateLimiting;
using AutoElite.API.Common;
using AutoElite.Application.Common;
using AutoElite.Infrastructure.Authentication;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;

namespace AutoElite.API.Hosting;

/// <summary>
/// How many requests a client may send in a minute, from the optional <see cref="SectionName"/> configuration section;
/// the defaults suit a school.
/// </summary>
internal sealed class RateLimitingSettings
{
    /// <summary>The configuration section of the settings.</summary>
    public const string SectionName = "RateLimiting";

    /// <summary>
    /// Gets or sets the requests a signed-in user, or an address without one, may send in a minute to the whole API.
    /// </summary>
    public int PermitsPerMinute { get; set; } = 600;

    /// <summary>
    /// Gets or sets the requests an address may send in a minute to the endpoints that anyone may call and that guess
    /// a password or send mail: signing in, accepting an invitation, the password reset and the public form.
    /// </summary>
    public int SensitivePermitsPerMinute { get; set; } = 20;

    /// <summary>Tells what is wrong with the settings, if anything.</summary>
    /// <returns>The problems, empty when the settings are valid.</returns>
    internal IEnumerable<string> Problems()
    {
        if (PermitsPerMinute < 1)
            yield return $"{SectionName}:PermitsPerMinute must be positive.";

        if (SensitivePermitsPerMinute < 1)
            yield return $"{SectionName}:SensitivePermitsPerMinute must be positive.";
    }
}

/// <summary>
/// The limits of the requests a client may send, so that one client cannot exhaust the server or guess passwords: a
/// limit on the whole API per signed-in user or per address, and a stricter one per address on the sensitive
/// endpoints, which carry <see cref="EnableRateLimitingAttribute"/> with <see cref="SensitivePolicy"/>. A request over
/// a limit is answered with 429 and the seconds to wait in Retry-After.
/// </summary>
internal static class ApiRateLimiting
{
    /// <summary>The policy of the sensitive endpoints.</summary>
    public const string SensitivePolicy = "sensitive";

    private static readonly TimeSpan _window = TimeSpan.FromMinutes(1);

    /// <summary>Adds the limits with the settings of the configuration.</summary>
    /// <param name="services">The services of the host.</param>
    /// <param name="configuration">The configuration of the host.</param>
    /// <returns>The services, for chaining.</returns>
    public static IServiceCollection AddApiRateLimiting(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<RateLimitingSettings>()
            .Bind(configuration.GetSection(RateLimitingSettings.SectionName))
            .ValidateOnStart();
        services.AddSingleton<IValidateOptions<RateLimitingSettings>>(
            new SettingsValidator<RateLimitingSettings>(settings => settings.Problems()));

        services.AddRateLimiter(_ => { });
        services.AddOptions<RateLimiterOptions>()
            .Configure<IOptions<RateLimitingSettings>>((limiter, settings) => Configure(limiter, settings.Value));

        return services;
    }

    private static void Configure(RateLimiterOptions limiter, RateLimitingSettings settings)
    {
        limiter.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
        limiter.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
            RateLimitPartition.GetFixedWindowLimiter(ClientOf(context), _ => Window(settings.PermitsPerMinute)));
        limiter.AddPolicy(
            SensitivePolicy,
            context => RateLimitPartition.GetFixedWindowLimiter(AddressOf(context), _ => Window(settings.SensitivePermitsPerMinute)));
        limiter.OnRejected = RejectAsync;
    }

    private static FixedWindowRateLimiterOptions Window(int permits) =>
        new() { PermitLimit = permits, Window = _window, QueueLimit = 0 };

    // The signed-in user, so that the users behind the one address of an office do not share a limit.
    private static string ClientOf(HttpContext context) =>
        context.User.FindFirstValue(AccessTokenClaims.UserId) is { } userId ? $"user:{userId}" : AddressOf(context);

    // The address of the client, which the forwarded headers of a known proxy give behind one.
    private static string AddressOf(HttpContext context) =>
        $"address:{context.Connection.RemoteIpAddress?.ToString() ?? "unknown"}";

    private static async ValueTask RejectAsync(OnRejectedContext context, CancellationToken cancellationToken)
    {
        var httpContext = context.HttpContext;
        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
        {
            httpContext.Response.Headers.RetryAfter =
                ((int)Math.Ceiling(retryAfter.TotalSeconds)).ToString(CultureInfo.InvariantCulture);
        }

        var problem = ApiProblems.Create(
            httpContext, ApplicationErrors.Request.TooManyRequests, StatusCodes.Status429TooManyRequests);
        await httpContext.Response.WriteAsJsonAsync(problem, options: null, "application/problem+json", cancellationToken);
    }
}
