using AutoElite.Client.Api;
using AutoElite.Client.Api.Models;
using AutoElite.Client.Offline;

namespace AutoElite.Mobile.Schedule;

/// <summary>
/// The notifications of the signed-in user: the latest of them, read from the API or, without a network, from the
/// device; how many are unread; and marking them read.
/// </summary>
/// <param name="cache">What the user last read.</param>
/// <param name="createApi">Creates the client of the API.</param>
public sealed class Notifications(OfflineCache cache, Func<AutoEliteApiClient> createApi)
{
    private const int Latest = 50;

    /// <summary>Reads the latest notifications, the latest first.</summary>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    /// <returns>The notifications.</returns>
    public async Task<Cached<IReadOnlyList<NotificationItem>>> ReadAsync(CancellationToken cancellationToken)
    {
        var page = await cache.GetAsync(
            "notifications",
            token => createApi().Api.V1.Notifications.GetAsync(request => request.QueryParameters.PageSize = Latest, token),
            PagedListOfNotificationItem.CreateFromDiscriminatorValue,
            cancellationToken);

        return new Cached<IReadOnlyList<NotificationItem>>(page.Value.Items ?? [], page.SavedAt, page.IsFromCache);
    }

    /// <summary>Counts the unread notifications, for the badge on the home page.</summary>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    /// <returns>The number of unread notifications.</returns>
    public async Task<int> CountUnreadAsync(CancellationToken cancellationToken) =>
        (await createApi().Api.V1.Notifications.UnreadCount.GetAsync(cancellationToken: cancellationToken))?.Count ?? 0;

    /// <summary>Marks a notification read.</summary>
    /// <param name="notificationId">The notification.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    /// <returns>A task that completes when the notification is read.</returns>
    public Task MarkReadAsync(Guid notificationId, CancellationToken cancellationToken) =>
        createApi().Api.V1.Notifications[notificationId].Read.PostAsync(cancellationToken: cancellationToken);

    /// <summary>Marks every notification read.</summary>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    /// <returns>A task that completes when the notifications are read.</returns>
    public Task MarkAllReadAsync(CancellationToken cancellationToken) =>
        createApi().Api.V1.Notifications.ReadAll.PostAsync(cancellationToken: cancellationToken);
}
