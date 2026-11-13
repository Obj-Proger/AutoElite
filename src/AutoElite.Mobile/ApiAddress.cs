namespace AutoElite.Mobile;

/// <summary>
/// The address of the API of the user's school, which they give on the first start and which is kept in the preferences
/// of the application. The address must use HTTPS; a debug build also takes HTTP, for the API of the development machine.
/// </summary>
internal static class ApiAddress
{
    private const string PreferenceKey = "autoelite.api-address";

    /// <summary>Gets the address the user gave, or <see langword="null"/> before they give one.</summary>
    public static Uri? Saved => TryParse(Preferences.Default.Get(PreferenceKey, string.Empty), out var address) ? address : null;

    /// <summary>
    /// Gets the address of the API for the client: the saved one, or, before the user gives one, the API of the
    /// development machine, which the application does not call until an address is saved.
    /// </summary>
    public static Uri Current => Saved ?? Development;

    /// <summary>Gets the address offered on the first start: the API of the development machine in a debug build.</summary>
    public static string Suggestion =>
#if DEBUG
        Development.ToString();
#else
        string.Empty;
#endif

    /// <summary>Keeps the address of the API.</summary>
    /// <param name="address">The address, as <see cref="TryParse"/> gave it.</param>
    public static void Save(Uri address) => Preferences.Default.Set(PreferenceKey, address.ToString());

    /// <summary>Forgets the address of the API, so that the user gives another one.</summary>
    public static void Clear() => Preferences.Default.Remove(PreferenceKey);

    /// <summary>Reads an address the user typed; the scheme may be left out, HTTPS is then assumed.</summary>
    /// <param name="text">The text.</param>
    /// <param name="address">The address, with a trailing slash, so that the paths of the API are added to it.</param>
    /// <returns><see langword="true"/> when the text is an address of an API the application may use.</returns>
    public static bool TryParse(string? text, out Uri address)
    {
        address = Development;
        var trimmed = text?.Trim() ?? string.Empty;
        if (trimmed.Length == 0)
            return false;

        if (!trimmed.Contains("://", StringComparison.Ordinal))
            trimmed = "https://" + trimmed;

        if (!Uri.TryCreate(trimmed, UriKind.Absolute, out var parsed) || !IsAllowedScheme(parsed.Scheme)
            || !string.IsNullOrEmpty(parsed.Query) || !string.IsNullOrEmpty(parsed.Fragment))
        {
            return false;
        }

        address = parsed.AbsolutePath.EndsWith('/') ? parsed : new Uri(parsed.AbsoluteUri + "/");

        return true;
    }

    private static Uri Development =>
        DeviceInfo.Current.Platform == DevicePlatform.Android
            ? new Uri("http://10.0.2.2:5080/")
            : new Uri("http://localhost:5080/");

    private static bool IsAllowedScheme(string scheme) =>
#if DEBUG
        scheme == Uri.UriSchemeHttps || scheme == Uri.UriSchemeHttp;
#else
        scheme == Uri.UriSchemeHttps;
#endif
}
