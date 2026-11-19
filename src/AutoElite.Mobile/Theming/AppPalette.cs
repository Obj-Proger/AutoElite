using AutoElite.Mobile.Resources.Styles;
#if ANDROID
using AndroidX.Core.View;
using Microsoft.Maui.Platform;
#endif

namespace AutoElite.Mobile.Theming;

/// <summary>
/// The theme of the application: the one of the device by default, or the dark or the light one the user chose in the
/// settings, which is kept in the preferences. It puts the colors of the theme into the resources of the application,
/// where the pages take them as dynamic resources, so that a change shows at once, and tells the views that draw
/// themselves in code to draw again.
/// </summary>
internal static class AppPalette
{
    private const string PreferenceKey = "autoelite.theme";

    private static Application? _application;
    private static bool _isApplying;

    /// <summary>Occurs when the colors of the application change.</summary>
    public static event EventHandler? Changed;

    /// <summary>Gets the theme the user chose.</summary>
    public static ThemeChoice Chosen =>
        Enum.TryParse<ThemeChoice>(Preferences.Default.Get(PreferenceKey, string.Empty), out var choice)
        && Enum.IsDefined(choice)
            ? choice
            : ThemeChoice.Device;

    /// <summary>Gets a value indicating whether the application shows the light colors.</summary>
    public static bool IsLight { get; private set; }

    /// <summary>Applies the chosen theme to the application and follows the theme of the device when it is chosen.</summary>
    /// <param name="application">The application.</param>
    public static void Start(Application application)
    {
        ArgumentNullException.ThrowIfNull(application);
        _application = application;
        application.RequestedThemeChanged += (_, _) =>
        {
            if (Chosen == ThemeChoice.Device)
                Apply();
        };
        Apply();
    }

    /// <summary>Keeps the theme the user chose and shows it at once.</summary>
    /// <param name="choice">The theme.</param>
    public static void Choose(ThemeChoice choice)
    {
        if (choice == ThemeChoice.Device)
            Preferences.Default.Remove(PreferenceKey);
        else
            Preferences.Default.Set(PreferenceKey, choice.ToString());

        Apply();
    }

    /// <summary>
    /// Gives the status and navigation bars of Android the background of the theme, with dark icons on the light one; the
    /// window exists only once the application starts.
    /// </summary>
    public static void ApplyToSystemBars()
    {
#if ANDROID
        if (Microsoft.Maui.ApplicationModel.Platform.CurrentActivity?.Window is not { } window)
            return;

        if (WindowCompat.GetInsetsController(window, window.DecorView) is { } controller)
        {
            controller.AppearanceLightStatusBars = IsLight;
            controller.AppearanceLightNavigationBars = IsLight;
        }

        // Since Android 15 the pages reach under the bars, which have no color of their own.
        if (!OperatingSystem.IsAndroidVersionAtLeast(35))
        {
            var background = ColorOf("Background").ToPlatform();
            window.SetStatusBarColor(background);
            window.SetNavigationBarColor(background);
        }
#endif
    }

    /// <summary>Gets a color of the current theme by its resource key, such as Accent.</summary>
    /// <param name="key">The resource key.</param>
    /// <returns>The color.</returns>
    public static Color ColorOf(string key) =>
        Application.Current?.Resources.TryGetValue(key, out var value) == true && value is Color color ? color : Colors.Gray;

    private static void Apply()
    {
        // Setting the theme of the system controls raises RequestedThemeChanged again.
        if (_application is not { } application || _isApplying)
            return;

        _isApplying = true;
        try
        {
            var choice = Chosen;
            application.UserAppTheme = choice switch
            {
                ThemeChoice.Dark => AppTheme.Dark,
                ThemeChoice.Light => AppTheme.Light,
                _ => AppTheme.Unspecified
            };
            IsLight = choice == ThemeChoice.Light
                || (choice == ThemeChoice.Device && application.PlatformAppTheme == AppTheme.Light);

            ResourceDictionary colors = IsLight ? new LightColors() : new DarkColors();
            foreach (var (key, value) in colors)
                application.Resources[key] = value;

            ApplyToSystemBars();
        }
        finally
        {
            _isApplying = false;
        }

        Changed?.Invoke(null, EventArgs.Empty);
    }
}
