using AutoElite.Client.Api;
using AutoElite.Client.Api.Models;
using AutoElite.Client.Offline;
using Microsoft.Kiota.Abstractions;

namespace AutoElite.Mobile.Schedule;

/// <summary>
/// The rules of the school the application follows before asking the API, read from its settings and kept on the
/// device: how long before a driving lesson a student may still cancel it, so that the application does not offer a
/// cancellation the school would refuse.
/// </summary>
/// <param name="cache">Keeps the settings on the device.</param>
/// <param name="createApi">Creates a client of the API.</param>
public sealed class SchoolRules(OfflineCache cache, Func<AutoEliteApiClient> createApi)
{
    /// <summary>Reads how long before a driving lesson a student may still cancel it.</summary>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    /// <returns>The notice, or <see langword="null"/> when the settings cannot be read, so that the API decides.</returns>
    public async Task<TimeSpan?> ReadStudentCancellationNoticeAsync(CancellationToken cancellationToken)
    {
        try
        {
            var settings = await cache.GetAsync(
                "settings",
                token => createApi().Api.V1.Settings.GetAsync(cancellationToken: token),
                SchoolSettingsDetails.CreateFromDiscriminatorValue,
                cancellationToken);

            return settings.Value.StudentCancellationNoticeMinutes is { } minutes ? TimeSpan.FromMinutes(minutes) : null;
        }
        catch (Exception exception) when (exception is ApiException or HttpRequestException or TaskCanceledException)
        {
            return null;
        }
    }
}
