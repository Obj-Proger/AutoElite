using AutoElite.Client.Api;
using AutoElite.Client.Api.Models;
using AutoElite.Client.Offline;

namespace AutoElite.Mobile.Schedule;

/// <summary>
/// The website of the school, where the application sends the user for what it leaves to it, such as drawing up the
/// contract of a lead. Its address comes from the API of the school, or, without a network, from the device; only a web
/// address is opened, over HTTPS outside a debug build, in the browser of the device.
/// </summary>
/// <param name="cache">What the user last read.</param>
/// <param name="createApi">Creates the client of the API.</param>
public sealed class Website(OfflineCache cache, Func<AutoEliteApiClient> createApi)
{
    /// <summary>Opens a page of the website in the browser of the device.</summary>
    /// <param name="path">The path of the page from the start page, such as <c>leads/{id}</c>; empty for the start page.</param>
    /// <returns><see langword="true"/> when the page was opened.</returns>
    public async Task<bool> OpenAsync(string path)
    {
        try
        {
            var site = await cache.GetAsync(
                "website",
                token => createApi().Api.V1.Settings.Website.GetAsync(cancellationToken: token),
                WebsiteDetails.CreateFromDiscriminatorValue,
                CancellationToken.None);
            if (!Uri.TryCreate(site.Value.Address, UriKind.Absolute, out var root) || !IsAllowedScheme(root.Scheme))
                return false;

            return await Browser.Default.OpenAsync(new Uri(root, path), BrowserLaunchMode.SystemPreferred);
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            return false;
        }
    }

    private static bool IsAllowedScheme(string scheme) =>
#if DEBUG
        scheme == Uri.UriSchemeHttps || scheme == Uri.UriSchemeHttp;
#else
        scheme == Uri.UriSchemeHttps;
#endif
}
