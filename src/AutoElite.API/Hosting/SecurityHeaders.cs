using AutoElite.API.Common;

namespace AutoElite.API.Hosting;

/// <summary>
/// The headers of every response that tell browsers how to treat it: not to guess its type, not to show it in a frame
/// of another site, not to send the address of the API in the Referer of a link, and, outside development, to reach
/// the API only over HTTPS for a year (HSTS, sent over HTTPS only). The answers of the authentication endpoints, which
/// carry tokens, are never stored by a browser or a proxy.
/// </summary>
internal static class SecurityHeaders
{
    /// <summary>The path of the endpoints whose answers carry tokens or the links of emails.</summary>
    public const string AuthenticationPath = "/" + ApiRoutes.Prefix + "/auth";

    // How long a browser keeps to HTTPS for the host after a response with HSTS.
    private static readonly TimeSpan _strictTransportSecurityMaxAge = TimeSpan.FromDays(365);

    /// <summary>Adds the settings of HSTS.</summary>
    /// <param name="services">The services of the host.</param>
    /// <returns>The services, for chaining.</returns>
    public static IServiceCollection AddSecurityHeaders(this IServiceCollection services) =>
        services.AddHsts(hsts => hsts.MaxAge = _strictTransportSecurityMaxAge);

    /// <summary>Adds the headers to every response, errors included; it runs right after the forwarded headers.</summary>
    /// <param name="app">The application.</param>
    /// <returns>The application, for chaining.</returns>
    public static WebApplication UseSecurityHeaders(this WebApplication app)
    {
        if (!app.Environment.IsDevelopment())
            app.UseHsts();

        app.Use(async (context, next) =>
        {
            var headers = context.Response.Headers;
            headers.XContentTypeOptions = "nosniff";
            headers.XFrameOptions = "DENY";
            headers.ContentSecurityPolicy = "frame-ancestors 'none'";
            headers["Referrer-Policy"] = "no-referrer";
            if (context.Request.Path.StartsWithSegments(AuthenticationPath, StringComparison.OrdinalIgnoreCase))
            {
                headers.CacheControl = "no-store";
                headers.Pragma = "no-cache";
            }

            await next(context);
        });

        return app;
    }
}
