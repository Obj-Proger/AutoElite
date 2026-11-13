using AutoElite.Client;
using AutoElite.Client.Api;
using AutoElite.Client.Authentication;
using AutoElite.Client.Offline;
using AutoElite.Mobile.Authentication;
using AutoElite.Mobile.Offline;
using AutoElite.Mobile.Pages;
using AutoElite.Mobile.Schedule;
using AutoElite.Mobile.Session;
#if DEBUG
using Microsoft.Extensions.Logging;
#endif

namespace AutoElite.Mobile;

/// <summary>
/// Builds the application: its pages, the client of the API, the session kept in secure storage and the offline data.
/// </summary>
public static class MauiProgram
{
    /// <summary>Creates the application.</summary>
    /// <returns>The application.</returns>
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder.UseMauiApp<App>().ConfigureFonts(fonts =>
        {
            // The fonts of Noir Modern (design/fonts), by the names the styles use.
            fonts.AddFont("Unbounded-Medium.ttf", "DisplayMedium");
            fonts.AddFont("Unbounded-SemiBold.ttf", "DisplaySemiBold");
            fonts.AddFont("Inter-Regular.ttf", "TextRegular");
            fonts.AddFont("Inter-Medium.ttf", "TextMedium");
            fonts.AddFont("Inter-SemiBold.ttf", "TextSemiBold");
        });

        // The user stays signed in between starts: the session is kept in the secure storage of the device.
        builder.Services.AddSingleton(SecureStorage.Default);
        builder.Services.AddSingleton<ISecretStore, SecureStorageSecretStore>();
        builder.Services.AddSingleton<IAuthSessionStore, SecretAuthSessionStore>();

        // The address of the API is the one the user gave, read again for every client of the API.
        builder.Services.AddAutoEliteClient(() => ApiAddress.Current);
        builder.Services.AddSingleton<Func<AutoEliteApiClient>>(provider => () => provider.GetRequiredService<AutoEliteApiClient>());

        // What the user read and the actions they took without a network, in the private storage of the application.
        builder.Services.AddAutoEliteOffline(Path.Combine(FileSystem.AppDataDirectory, "offline.db3"));
        builder.Services.AddSingleton(Connectivity.Current);
        builder.Services.AddSingleton<SynchronizationTrigger>();

        builder.Services.AddSingleton<UserSession>();
        builder.Services.AddSingleton<StudentSchedule>();
        builder.Services.AddSingleton<DrivingBooking>();
        builder.Services.AddSingleton<InstructorSchedule>();
        builder.Services.AddSingleton<PracticeTests>();
        builder.Services.AddSingleton<AppShell>();
        builder.Services.AddTransient<StartPage>();
        builder.Services.AddTransient<SchoolPage>();
        builder.Services.AddTransient<SchoolViewModel>();
        builder.Services.AddTransient<SignInPage>();
        builder.Services.AddTransient<SignInViewModel>();
        builder.Services.AddTransient<HomePage>();
        builder.Services.AddTransient<HomeViewModel>();
        builder.Services.AddTransient<BookingPage>();
        builder.Services.AddTransient<BookingViewModel>();
        builder.Services.AddTransient<AttendancePage>();
        builder.Services.AddTransient<AttendanceViewModel>();
        builder.Services.AddTransient<CompletionPage>();
        builder.Services.AddTransient<CompletionViewModel>();
        builder.Services.AddTransient<TestsPage>();
        builder.Services.AddTransient<TestsViewModel>();
        builder.Services.AddTransient<TestSessionPage>();
        builder.Services.AddTransient<TestSessionViewModel>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
