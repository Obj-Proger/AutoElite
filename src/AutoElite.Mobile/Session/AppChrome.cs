using AutoElite.Client.Api.Models;
using AutoElite.Client.Realtime;
using AutoElite.Mobile.Schedule;
using Microsoft.Kiota.Abstractions;

namespace AutoElite.Mobile.Session;

/// <summary>
/// What the frame around the tabs shows of the signed-in user: their name, for the avatar, and the counts of new
/// things: unread notifications on the bell, unread messages on the tab of chats and, for a manager or an
/// administrator, new leads on the tab of leads. It reads them again when a tab appears and when the hub of the API
/// tells of a new notification or message; without a network the last counts stay.
/// </summary>
public sealed class AppChrome
{
    // How long the counts are shown before a tab that appears reads them again.
    private static readonly TimeSpan _freshness = TimeSpan.FromSeconds(20);

    private readonly UserSession _session;
    private readonly Notifications _notifications;
    private readonly Chats _chats;
    private readonly LeadBook _leads;
    private bool _isRefreshing;
    private DateTimeOffset _refreshedAt = DateTimeOffset.MinValue;

    /// <summary>Initializes a new instance of the <see cref="AppChrome"/> class.</summary>
    /// <param name="session">The user of the application.</param>
    /// <param name="notifications">The notifications of the user.</param>
    /// <param name="chats">The chats of the user.</param>
    /// <param name="leads">The leads of the school.</param>
    /// <param name="realtime">The connection through which new notifications and messages arrive at once.</param>
    public AppChrome(UserSession session, Notifications notifications, Chats chats, LeadBook leads, RealtimeConnection realtime)
    {
        ArgumentNullException.ThrowIfNull(realtime);
        _session = session;
        _notifications = notifications;
        _chats = chats;
        _leads = leads;
        realtime.NotificationReceived += (_, _) => MainThread.BeginInvokeOnMainThread(async () => await RefreshAsync(force: true));
        realtime.ChatMessageReceived += (_, _) => MainThread.BeginInvokeOnMainThread(async () => await RefreshAsync(force: true));
    }

    /// <summary>Occurs on the main thread when the name or a count changes.</summary>
    public event EventHandler? Changed;

    /// <summary>Gets the full name of the user.</summary>
    public string UserName { get; private set; } = string.Empty;

    /// <summary>Gets the number of unread notifications.</summary>
    public int NotificationCount { get; private set; }

    /// <summary>Gets the number of unread messages in every chat.</summary>
    public int ChatCount { get; private set; }

    /// <summary>Gets the number of new leads, for a manager or an administrator.</summary>
    public int LeadCount { get; private set; }

    /// <summary>
    /// Reads the name and the counts again, unless they were read a moment ago or are being read; the hub of the API
    /// forces it for a new notification or message.
    /// </summary>
    /// <param name="force">Whether to read them even if they were read a moment ago.</param>
    /// <returns>A task that completes when the counts are read.</returns>
    public async Task RefreshAsync(bool force = false)
    {
        if (_isRefreshing || (!force && DateTimeOffset.UtcNow - _refreshedAt < _freshness))
        {
            Changed?.Invoke(this, EventArgs.Empty);

            return;
        }

        _isRefreshing = true;
        try
        {
            var roles = await ReadUserAsync();
            NotificationCount = await CountAsync(_notifications.CountUnreadAsync, NotificationCount);
            ChatCount = await CountAsync(CountUnreadMessagesAsync, ChatCount);
            LeadCount = roles.Contains(UserRole.Manager) || roles.Contains(UserRole.Administrator)
                ? await CountAsync(async token => (await _leads.ReadNewAsync(token)).Value.Count, LeadCount)
                : 0;
            _refreshedAt = DateTimeOffset.UtcNow;
            Changed?.Invoke(this, EventArgs.Empty);
        }
        finally
        {
            _isRefreshing = false;
        }
    }

    // A count read from the API, or the last one when the API cannot be reached.
    private static async Task<int> CountAsync(Func<CancellationToken, Task<int>> count, int last)
    {
        try
        {
            return await count(CancellationToken.None);
        }
        catch (Exception exception) when (exception is ApiException or HttpRequestException or TaskCanceledException)
        {
            return last;
        }
    }

    private async Task<int> CountUnreadMessagesAsync(CancellationToken cancellationToken) =>
        (await _chats.ReadChatsAsync(cancellationToken)).Value.Sum(chat => chat.UnreadCount ?? 0);

    // The name of the user and their roles, from the API or the device.
    private async Task<IReadOnlyCollection<UserRole?>> ReadUserAsync()
    {
        try
        {
            var profile = (await _session.GetProfileAsync(CancellationToken.None)).Value;
            UserName = string.Join(' ', new[] { profile.FirstName, profile.LastName }.Where(part => !string.IsNullOrWhiteSpace(part)));

            return profile.Roles ?? [];
        }
        catch (Exception exception) when (exception is ApiException or HttpRequestException or TaskCanceledException)
        {
            return [];
        }
    }
}
