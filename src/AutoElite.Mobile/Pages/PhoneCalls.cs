namespace AutoElite.Mobile.Pages;

/// <summary>Calls a phone number from the application.</summary>
internal static class PhoneCalls
{
    /// <summary>
    /// Calls with the dialer of the device; Windows has none of its own, so the tel link goes to an application that
    /// makes calls, such as Phone Link, and without one the number is copied for the user to call from their phone.
    /// </summary>
    /// <param name="phone">The number.</param>
    /// <returns><see langword="true"/> if the call was passed on; <see langword="false"/> if the number was copied.</returns>
    public static async Task<bool> CallAsync(string phone)
    {
        if (PhoneDialer.Default.IsSupported)
        {
            PhoneDialer.Default.Open(phone);

            return true;
        }

        if (await Launcher.Default.TryOpenAsync(new Uri("tel:" + phone)))
            return true;

        await Clipboard.Default.SetTextAsync(phone);

        return false;
    }

    /// <summary>The initials of a full name, its first two words: the last and the first name.</summary>
    /// <param name="name">The full name.</param>
    /// <returns>The initials in capitals.</returns>
    public static string InitialsOf(string name) =>
        string.Concat(name.Split(' ', StringSplitOptions.RemoveEmptyEntries).Take(2).Select(part => part[0]))
            .ToUpper(System.Globalization.CultureInfo.CurrentCulture);
}
