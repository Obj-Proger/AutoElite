using System.Globalization;
using AutoElite.Client.Api.Models;
using AutoElite.Mobile.Localization;
using AutoElite.Mobile.Schedule;
using Microsoft.Kiota.Abstractions;

namespace AutoElite.Mobile.Pages;

/// <summary>
/// The notifications of the user, the latest first, from the API or, without a network, from the device; an unread one
/// is marked read when the user touches it, or all of them at once.
/// </summary>
public sealed class NotificationsViewModel : ViewModel
{
    private readonly Notifications _notifications;

    /// <summary>Initializes a new instance of the <see cref="NotificationsViewModel"/> class.</summary>
    /// <param name="notifications">The notifications.</param>
    public NotificationsViewModel(Notifications notifications)
    {
        _notifications = notifications;
        ReadAllCommand = new Command(async () => await ReadAllAsync());
        RefreshCommand = new Command(async () => await RefreshAsync());
    }

    /// <summary>Gets the notifications.</summary>
    public IReadOnlyList<NotificationEntry> Items
    {
        get;
        private set
        {
            if (SetProperty(ref field, value))
                OnPropertyChanged(nameof(HasUnread));
        }
    } = [];

    /// <summary>Gets a value indicating whether some notifications are unread.</summary>
    public bool HasUnread => Items.Any(item => item.IsUnread);

    /// <summary>Gets that there are no notifications, or that they are from the device, or <see langword="null"/>.</summary>
    public string? Notice
    {
        get;
        private set
        {
            if (SetProperty(ref field, value))
                OnPropertyChanged(nameof(HasNotice));
        }
    }

    /// <summary>Gets a value indicating whether there is a notice to show.</summary>
    public bool HasNotice => Notice is not null;

    /// <summary>Gets or sets a value indicating whether the page is being refreshed by pulling it down.</summary>
    public bool IsRefreshing
    {
        get;
        set => SetProperty(ref field, value);
    }

    /// <summary>Gets the command that marks every notification read.</summary>
    public Command ReadAllCommand { get; }

    /// <summary>Gets the command that reads the notifications again.</summary>
    public Command RefreshCommand { get; }

    /// <summary>Reads the notifications.</summary>
    /// <returns>A task that completes when the page shows them.</returns>
    public async Task LoadAsync()
    {
        try
        {
            var notifications = await _notifications.ReadAsync(CancellationToken.None);
            Items = [.. notifications.Value.Select(Entry).OfType<NotificationEntry>()];
            Notice = notifications.IsFromCache
                ? AppTexts.Format(
                    "Notifications.Offline", notifications.SavedAt.ToLocalTime().ToString("g", CultureInfo.CurrentCulture))
                : Items.Count == 0 ? AppTexts.Get("Notifications.Empty") : null;
        }
        catch (Exception exception) when (exception is ApiException or HttpRequestException)
        {
            Items = [];
            Notice = AppTexts.Get("Notifications.Unavailable");
        }
    }

    private NotificationEntry? Entry(NotificationItem item)
    {
        if (item is not { Id: { } id, CreatedAtUtc: { } createdAt })
            return null;

        return new NotificationEntry
        {
            Title = item.Title ?? string.Empty,
            Body = item.Body,
            Time = createdAt.ToLocalTime().ToString("g", CultureInfo.CurrentCulture),
            IsUnread = item.ReadAtUtc is null,
            ReadCommand = new Command(async () => await ReadAsync(id, item.ReadAtUtc is null))
        };
    }

    private async Task ReadAsync(Guid notificationId, bool isUnread)
    {
        if (!isUnread)
            return;

        try
        {
            await _notifications.MarkReadAsync(notificationId, CancellationToken.None);
            await LoadAsync();
        }
        catch (Exception exception) when (exception is ApiException or HttpRequestException)
        {
            // Without a network the notification stays unread until the next try.
            Notice = AppTexts.Get("Notifications.Unavailable");
        }
    }

    private async Task ReadAllAsync()
    {
        try
        {
            await _notifications.MarkAllReadAsync(CancellationToken.None);
            await LoadAsync();
        }
        catch (Exception exception) when (exception is ApiException or HttpRequestException)
        {
            Notice = AppTexts.Get("Notifications.Unavailable");
        }
    }

    private async Task RefreshAsync()
    {
        try
        {
            await LoadAsync();
        }
        finally
        {
            IsRefreshing = false;
        }
    }
}
