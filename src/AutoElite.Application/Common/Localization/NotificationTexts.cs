using System.Globalization;
using System.Resources;

namespace AutoElite.Application.Common.Localization;

/// <summary>
/// The texts of in-app notifications, from the <c>NotificationTexts</c> resources: English by default and a
/// translation for every other supported language. Each notification has a <c>{Name}.Title</c> and a
/// <c>{Name}.Body</c> text; the constants are the names.
/// </summary>
internal static class NotificationTexts
{
    /// <summary>
    /// A payment of the student is confirmed: <c>{0}</c> is the amount, <c>{1}</c> the currency, <c>{2}</c> the
    /// contract number.
    /// </summary>
    public const string PaymentConfirmed = "PaymentConfirmed";

    /// <summary>
    /// A payment of the student is refunded: <c>{0}</c> is the amount, <c>{1}</c> the currency, <c>{2}</c> the
    /// contract number.
    /// </summary>
    public const string PaymentRefunded = "PaymentRefunded";

    /// <summary>Gets the notification text resources.</summary>
    internal static ResourceManager Resources { get; } =
        new("AutoElite.Application.Common.Localization.NotificationTexts", typeof(NotificationTexts).Assembly);

    /// <summary>Gets the title of a notification in the culture. Titles have no placeholders.</summary>
    /// <param name="name">The name of the notification, one of the constants of this class.</param>
    /// <param name="culture">The culture of the recipient.</param>
    public static string Title(string name, CultureInfo culture) => Get($"{name}.Title", culture);

    /// <summary>Gets the body of a notification in the culture and fills in its placeholders.</summary>
    /// <param name="name">The name of the notification, one of the constants of this class.</param>
    /// <param name="culture">The culture of the recipient.</param>
    /// <param name="arguments">The values of the placeholders, in order.</param>
    public static string Body(string name, CultureInfo culture, params object?[] arguments) =>
        string.Format(culture, Get($"{name}.Body", culture), arguments);

    private static string Get(string key, CultureInfo culture) =>
        Resources.GetString(key, culture) ?? throw new InvalidOperationException($"The notification text '{key}' is missing.");
}
