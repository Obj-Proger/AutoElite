namespace AutoElite.Mobile;

/// <summary>
/// The address of the API of the school, kept in the preferences of the application; until the user gives one, the API
/// of the development machine, which the Android emulator reaches at 10.0.2.2.
/// </summary>
internal static class ApiAddress
{
    private const string PreferenceKey = "autoelite.api-address";

    /// <summary>Gets the address of the API.</summary>
    public static Uri Current =>
        Uri.TryCreate(Preferences.Default.Get(PreferenceKey, string.Empty), UriKind.Absolute, out var address)
            ? address
            : Development;

    private static Uri Development =>
        DeviceInfo.Current.Platform == DevicePlatform.Android
            ? new Uri("http://10.0.2.2:5080/")
            : new Uri("http://localhost:5080/");
}
