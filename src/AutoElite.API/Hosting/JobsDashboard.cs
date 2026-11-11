using System.Security.Claims;
using AutoElite.API.Common;
using AutoElite.Application.Common;
using AutoElite.Infrastructure.Authentication;
using Hangfire;
using Hangfire.Dashboard;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace AutoElite.API.Hosting;

/// <summary>
/// The Hangfire dashboard of the background jobs at <see cref="Path"/>, for administrators only. A browser cannot send
/// the access token of the API when it opens a page, so an administrator first opens a session of the dashboard with
/// the token at <see cref="SessionPath"/>: it sets a cookie that only the pages of the dashboard receive, HttpOnly and
/// SameSite=Strict, which lasts <see cref="SessionLifetime"/> and is not renewed. The actions of the dashboard are
/// also protected by antiforgery tokens.
/// </summary>
internal static class JobsDashboard
{
    /// <summary>The address of the dashboard.</summary>
    public const string Path = "/jobs";

    /// <summary>The address that opens a session of the dashboard.</summary>
    public const string SessionPath = "/" + ApiRoutes.Prefix + "/jobs-dashboard/session";

    /// <summary>The authentication scheme of the cookie of the dashboard.</summary>
    public const string Scheme = "JobsDashboard";

    /// <summary>How long a session of the dashboard lasts.</summary>
    public static readonly TimeSpan SessionLifetime = TimeSpan.FromMinutes(30);

    /// <summary>Adds the cookie of the sessions and the antiforgery tokens of the dashboard.</summary>
    /// <param name="services">The services of the host.</param>
    /// <returns>The services, for chaining.</returns>
    public static IServiceCollection AddJobsDashboard(this IServiceCollection services)
    {
        services.AddAuthentication().AddCookie(Scheme, cookie =>
        {
            cookie.Cookie.Name = "AutoElite.Jobs";
            cookie.Cookie.Path = Path;
            cookie.Cookie.HttpOnly = true;
            cookie.Cookie.SameSite = SameSiteMode.Strict;
            cookie.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
            cookie.ExpireTimeSpan = SessionLifetime;
            cookie.SlidingExpiration = false;

            // The dashboard answers itself; nothing redirects to a sign-in page, which the API does not have.
            cookie.Events.OnRedirectToLogin = context => Answer(context, StatusCodes.Status401Unauthorized);
            cookie.Events.OnRedirectToAccessDenied = context => Answer(context, StatusCodes.Status403Forbidden);
        });
        services.AddAntiforgery(antiforgery =>
        {
            antiforgery.Cookie.Path = Path;
            antiforgery.Cookie.SameSite = SameSiteMode.Strict;
        });

        return services;
    }

    /// <summary>Maps the dashboard and the address that opens its sessions.</summary>
    /// <param name="app">The host.</param>
    public static void MapJobsDashboard(this WebApplication app)
    {
        app.MapPost(SessionPath, OpenSessionAsync)
            .WithTags("JobsDashboard")
            .WithSummary("Opens a session of the dashboard of the background jobs for the signed-in administrator.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status401Unauthorized, "application/problem+json")
            .ProducesProblem(StatusCodes.Status403Forbidden, "application/problem+json");

        app.MapHangfireDashboard(Path, new DashboardOptions
        {
            Authorization = [],
            AsyncAuthorization = [new AdministratorSessionFilter()],
            DashboardTitle = "AutoElite",
            DisplayStorageConnectionString = false,
            AppPath = null
        });
    }

    private static Task Answer(RedirectContext<CookieAuthenticationOptions> context, int statusCode)
    {
        context.Response.StatusCode = statusCode;

        return Task.CompletedTask;
    }

    private static async Task<IResult> OpenSessionAsync(HttpContext context, ICurrentUser currentUser)
    {
        if (currentUser.UserId is not { } userId)
            return Problem(context, ApplicationErrors.Auth.Unauthenticated);

        if (!currentUser.Roles.Contains(UserRole.Administrator))
            return Problem(context, ApplicationErrors.Auth.Forbidden);

        Claim[] claims =
        [
            new(AccessTokenClaims.UserId, userId.ToString()),
            new(AccessTokenClaims.Role, nameof(UserRole.Administrator))
        ];
        var identity = new ClaimsIdentity(claims, Scheme, AccessTokenClaims.UserId, AccessTokenClaims.Role);
        await context.SignInAsync(Scheme, new ClaimsPrincipal(identity));

        return Results.NoContent();
    }

    private static IResult Problem(HttpContext context, Error error)
    {
        var problem = ApiProblems.Create(context, error);

        return Results.Json(problem, contentType: "application/problem+json", statusCode: problem.Status);
    }

    /// <summary>Lets in the requests with the cookie of a session that an administrator opened.</summary>
    private sealed class AdministratorSessionFilter : IDashboardAsyncAuthorizationFilter
    {
        public async Task<bool> AuthorizeAsync(DashboardContext context)
        {
            var result = await context.GetHttpContext().AuthenticateAsync(Scheme);

            return result is { Succeeded: true, Principal: { } principal } && principal.IsInRole(nameof(UserRole.Administrator));
        }
    }
}
