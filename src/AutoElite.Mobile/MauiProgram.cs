using AutoElite.Client;
using AutoElite.Client.Authentication;
using AutoElite.Mobile.Authentication;
#if DEBUG
using Microsoft.Extensions.Logging;
#endif

namespace AutoElite.Mobile;

/// <summary>Builds the application: its pages, the client of the API and the session kept in secure storage.</summary>
public static class MauiProgram
{
    /// <summary>Creates the application.</summary>
    /// <returns>The application.</returns>
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder.UseMauiApp<App>();

        // The user stays signed in between starts: the session is kept in the secure storage of the device.
        builder.Services.AddSingleton(SecureStorage.Default);
        builder.Services.AddSingleton<ISecretStore, SecureStorageSecretStore>();
        builder.Services.AddSingleton<IAuthSessionStore, SecretAuthSessionStore>();
        builder.Services.AddAutoEliteClient(ApiAddress.Current);

        builder.Services.AddSingleton<AppShell>();
        builder.Services.AddTransient<MainPage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
