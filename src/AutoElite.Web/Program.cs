using AutoElite.Client;
using AutoElite.Client.Api;
using AutoElite.Client.Authentication;
using AutoElite.Web;
using AutoElite.Web.Authentication;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// The API of the school: Api:BaseUrl in wwwroot/appsettings.json, or the site itself when the API serves it.
var apiAddress = builder.Configuration["Api:BaseUrl"] is { Length: > 0 } configured
    ? new Uri(configured)
    : new Uri(builder.HostEnvironment.BaseAddress);

// The session: the refresh token in the HttpOnly cookie of the API, which the requests of the session send, the access
// token in memory, renewed by one tab of the browser at a time.
builder.Services.AddSingleton<ISessionRenewalLock, BrowserTabsLock>();
builder.Services.AddAutoEliteWebsiteClient(
    apiAddress, authentication => authentication.AddHttpMessageHandler(() => new BrowserCookiesHandler()));
builder.Services.AddSingleton<Func<AutoEliteApiClient>>(provider => () => provider.GetRequiredService<AutoEliteApiClient>());

// Who is signed in, for the pages that need signing in and for the frame of the site.
builder.Services.AddAuthorizationCore();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddSingleton<AuthenticationStateProvider, WebAuthenticationState>();

builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");

await builder.Build().RunAsync();
