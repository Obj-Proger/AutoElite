using System.Globalization;

namespace AutoElite.Application.Common.Localization;

/// <summary>Maps the languages users can choose to the cultures used for texts.</summary>
public static class SupportedLanguages
{
    /// <summary>The language used when the user has not chosen one and the request does not name one.</summary>
    public const Language Default = Language.Russian;

    /// <summary>Gets the cultures of all supported languages, the default first.</summary>
    public static IReadOnlyList<CultureInfo> Cultures { get; } =
        [CultureInfo.GetCultureInfo("ru"), CultureInfo.GetCultureInfo("en")];

    /// <summary>Gets the culture of a language.</summary>
    /// <param name="language">The language.</param>
    /// <exception cref="ArgumentOutOfRangeException">The language is not defined.</exception>
    public static CultureInfo ToCulture(this Language language) => language switch
    {
        Language.Russian => Cultures[0],
        Language.English => Cultures[1],
        _ => throw new ArgumentOutOfRangeException(nameof(language), language, "The language is not supported.")
    };

    /// <summary>
    /// Finds the language of a culture by its two-letter code, so that <c>ru-RU</c> and <c>en-GB</c> are recognized.
    /// </summary>
    /// <param name="culture">The culture, for example from the request headers.</param>
    /// <returns>The language, or <see langword="null"/> if the culture is not supported.</returns>
    public static Language? FromCulture(CultureInfo culture)
    {
        ArgumentNullException.ThrowIfNull(culture);

        return culture.TwoLetterISOLanguageName switch
        {
            "ru" => Language.Russian,
            "en" => Language.English,
            _ => null
        };
    }
}
