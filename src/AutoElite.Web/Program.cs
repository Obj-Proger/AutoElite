using AutoElite.Client;
using AutoElite.Web;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// The API of the school: Api:BaseUrl in wwwroot/appsettings.json, or the site itself when the API serves it.
var apiAddress = builder.Configuration["Api:BaseUrl"] is { Length: > 0 } configured
    ? new Uri(configured)
    : new Uri(builder.HostEnvironment.BaseAddress);
builder.Services.AddAutoEliteClient(apiAddress);
builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");

await builder.Build().RunAsync();
