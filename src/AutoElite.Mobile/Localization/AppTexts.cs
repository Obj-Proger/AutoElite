using System.Globalization;
using System.Resources;

namespace AutoElite.Mobile.Localization;

/// <summary>
/// The texts of the application, from the <c>AppTexts</c> resources in the language of the device: English by default
/// and a translation for every other supported language. Pages take them with <see cref="TextExtension"/>.
/// </summary>
internal static class AppTexts
{
    private static readonly ResourceManager _resources =
        new("AutoElite.Mobile.Resources.Strings.AppTexts", typeof(AppTexts).Assembly);

    /// <summary>Gets a text in the language of the device.</summary>
    /// <param name="key">The key of the text, such as <c>SignIn.Title</c>.</param>
    /// <returns>The text; the key itself when the text is missing, which the architecture tests rule out.</returns>
    public static string Get(string key) => _resources.GetString(key, CultureInfo.CurrentUICulture) ?? key;

    /// <summary>Gets a text in the language of the device and fills in its placeholders.</summary>
    /// <param name="key">The key of the text.</param>
    /// <param name="arguments">The values of the placeholders, in order.</param>
    /// <returns>The text.</returns>
    public static string Format(string key, params object?[] arguments) =>
        string.Format(CultureInfo.CurrentCulture, Get(key), arguments);
}
