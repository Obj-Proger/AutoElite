using System.Text.Json.Serialization;
using AutoElite.API.Authentication;
using AutoElite.API.Common;
using AutoElite.API.Hosting;
using AutoElite.API.Realtime;
using AutoElite.API.OpenApi;
using AutoElite.Application;
using AutoElite.Application.Common.Localization;
using AutoElite.Infrastructure;
using AutoElite.Infrastructure.Persistence;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// One JSON object per line for the log collectors of a deployment (appsettings.json), readable lines when developing.
if (builder.Environment.IsDevelopment())
{
    builder.Logging.AddSimpleConsole(options =>
    {
        options.SingleLine = true;
        options.TimestampFormat = "HH:mm:ss ";
    });
}

// The application, the infrastructure, and the caller of each request for both.
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, HttpCurrentUser>();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// JSON with enumerations as their names, in the controllers and in what the host writes itself, such as the OpenAPI
// document and the problem details of exceptions; every error is problem details (ApiProblems).
builder.Services.AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()))
    .ConfigureApiBehaviorOptions(options =>
    {
        options.InvalidModelStateResponseFactory = ApiProblems.FromModelState;
        options.ClientErrorMapping[StatusCodes.Status429TooManyRequests] = new ClientErrorData
        {
            Title = "Too Many Requests",
            Link = "https://www.rfc-editor.org/rfc/rfc6585#section-4"
        };
    });
builder.Services.ConfigureHttpJsonOptions(options => options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

// Messages are in the language the client asks for in Accept-Language. The clients of the school send the language of
// their user; a request without one, from a script or another system, is answered in the default language.
builder.Services.AddRequestLocalization(options =>
{
    var cultures = SupportedLanguages.Cultures.ToList();
    options.DefaultRequestCulture = new RequestCulture(SupportedLanguages.Default.ToCulture());
    options.SupportedCultures = cultures;
    options.SupportedUICultures = cultures;
    options.RequestCultureProviders = [new AcceptLanguageHeaderRequestCultureProvider()];
});

// Behind a reverse proxy the client and its scheme come from the forwarded headers of the trusted proxies; the pages of
// the web client may call the API from the browser; and every client has limits on the requests it sends.
builder.Services.AddTrustedProxies(builder.Configuration);
builder.Services.AddApiCors(builder.Configuration);
builder.Services.AddApiRateLimiting(builder.Configuration);

// Administrators watch and retry the background jobs in the Hangfire dashboard.
builder.Services.AddJobsDashboard();

// Signed-in users receive notifications and chat messages at once through the SignalR hub, which requires them to be
// signed in; everything else checks access in the application.
builder.Services.AddAuthorization();
builder.Services.AddApiRealtime();

// The OpenAPI document is the public contract (D-002); the database decides whether the host is ready.
builder.Services.AddOpenApi(options => options.AddDocumentTransformer<BearerSecuritySchemeTransformer>());
builder.Services.AddHealthChecks().AddDbContextCheck<AutoEliteDbContext>(tags: [HealthEndpoints.ReadyTag]);

var app = builder.Build();

app.UseForwardedHeaders();
app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseCors();
app.UseRequestLocalization();
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

app.MapControllers();
app.MapJobsDashboard();
app.MapApiRealtime();
app.MapOpenApi();
if (app.Environment.IsDevelopment())
    app.MapScalarApiReference();

// The probes of the orchestrator are not limited, so that a busy host is not taken for a broken one.
app.MapHealthChecks(HealthEndpoints.Live, new HealthCheckOptions { Predicate = _ => false }).DisableRateLimiting();
app.MapHealthChecks(
        HealthEndpoints.Ready, new HealthCheckOptions { Predicate = check => check.Tags.Contains(HealthEndpoints.ReadyTag) })
    .DisableRateLimiting();

await app.RunAsync();
