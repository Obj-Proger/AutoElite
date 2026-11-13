using System.Globalization;

namespace AutoElite.Mobile.Localization;

/// <summary>
/// The language of the application: the language of the device by default, or one the user chose in the settings,
/// which is kept in the preferences and applied when the application starts.
/// </summary>
internal static class AppLanguage
{
    private const string PreferenceKey = "autoelite.language";

    /// <summary>Gets the languages the user can choose, by culture name; empty for the language of the device.</summary>
    public static IReadOnlyList<string> Cultures { get; } = [string.Empty, "en", "ru"];

    /// <summary>Gets the language the application started with, which a new choice changes only after a restart.</summary>
    public static string Started { get; private set; } = string.Empty;

    /// <summary>Gets the language the user chose, or empty for the language of the device.</summary>
    public static string Chosen => Preferences.Default.Get(PreferenceKey, string.Empty);

    /// <summary>Keeps the language the user chose; it applies from the next start of the application.</summary>
    /// <param name="culture">The culture name, or empty for the language of the device.</param>
    public static void Choose(string culture)
    {
        if (culture.Length == 0)
            Preferences.Default.Remove(PreferenceKey);
        else
            Preferences.Default.Set(PreferenceKey, culture);
    }

    /// <summary>Applies the chosen language to the texts and the formats of the application, before the pages exist.</summary>
    public static void Apply()
    {
        Started = Chosen;
        if (Started.Length == 0 || !Cultures.Contains(Started))
            return;

        var culture = CultureInfo.GetCultureInfo(Started);
        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;
        CultureInfo.CurrentCulture = culture;
        CultureInfo.CurrentUICulture = culture;
    }
}
