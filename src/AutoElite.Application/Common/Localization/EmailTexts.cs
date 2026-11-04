using System.Globalization;
using System.Resources;

namespace AutoElite.Application.Common.Localization;

/// <summary>
/// The texts of the emails the system sends, from the <c>EmailTexts</c> resources: English by default and a
/// translation for every other supported language.
/// </summary>
internal static class EmailTexts
{
    /// <summary>The subject of the invitation email. No placeholders.</summary>
    public const string InvitationSubject = "Invitation.Subject";

    /// <summary>The body of the invitation email: <c>{0}</c> is the first name, <c>{1}</c> the link.</summary>
    public const string InvitationBody = "Invitation.Body";

    /// <summary>Gets the email text resources.</summary>
    internal static ResourceManager Resources { get; } =
        new("AutoElite.Application.Common.Localization.EmailTexts", typeof(EmailTexts).Assembly);

    /// <summary>Gets a text in the culture and fills in its placeholders.</summary>
    /// <param name="key">The key of the text, one of the constants of this class.</param>
    /// <param name="culture">The culture of the recipient.</param>
    /// <param name="arguments">The values of the placeholders, in order.</param>
    /// <exception cref="InvalidOperationException">The text is missing, which the resource tests rule out.</exception>
    public static string Format(string key, CultureInfo culture, params object?[] arguments)
    {
        var template = Resources.GetString(key, culture)
            ?? throw new InvalidOperationException($"The email text '{key}' is missing.");

        return string.Format(culture, template, arguments);
    }
}
