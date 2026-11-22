using System.Globalization;
using AutoElite.Client.Api.Models;
using AutoElite.Mobile.Controls;
using AutoElite.Mobile.Localization;
using AutoElite.Mobile.Schedule;
using Microsoft.Kiota.Abstractions;

namespace AutoElite.Mobile.Pages;

/// <summary>
/// The notifications of the user by days, the latest first, all of them or the unread ones, from the API or, without a
/// network, from the device. Each has the icon of what it is about; touching one marks it read and opens what it is
/// about, such as the schedule, the chats or the leads, and all of them are marked read at once.
/// </summary>
public sealed class NotificationsViewModel : ViewModel
{
    private readonly Notifications _notifications;
    private readonly TimeProvider _timeProvider;
    private IReadOnlyList<NotificationItem> _read = [];

    /// <summary>Initializes a new instance of the <see cref="NotificationsViewModel"/> class.</summary>
    /// <param name="notifications">The notifications.</param>
    /// <param name="timeProvider">The clock, for how long ago a notification came.</param>
    public NotificationsViewModel(Notifications notifications, TimeProvider timeProvider)
    {
        _notifications = notifications;
        _timeProvider = timeProvider;
        ReadAllCommand = new Command(async () => await ReadAllAsync(), () => UnreadCount > 0);
        RefreshCommand = new Command(async () => await RefreshAsync());
        ShowAllCommand = new Command(() => ShowsUnreadOnly = false);
        ShowUnreadCommand = new Command(() => ShowsUnreadOnly = true);
    }

    /// <summary>Gets the notifications shown, by days.</summary>
    public IReadOnlyList<NotificationGroup> Groups
    {
        get;
        private set
        {
            if (SetProperty(ref field, value))
                OnPropertyChanged(nameof(IsEmpty));
        }
    } = [];

    /// <summary>Gets a value indicating whether there is nothing to show, once the notifications are read.</summary>
    public bool IsEmpty => Groups.Count == 0 && Notice is null;

    /// <summary>Gets what the page says when there is nothing to show: no notifications, or none unread.</summary>
    public string EmptyText => ShowsUnreadOnly ? AppTexts.Get("Notifications.NoUnread") : AppTexts.Get("Notifications.Empty");

    /// <summary>Gets the number of unread notifications.</summary>
    public int UnreadCount
    {
        get;
        private set
        {
            if (!SetProperty(ref field, value))
                return;

            OnPropertyChanged(nameof(UnreadFilter));
            ReadAllCommand.ChangeCanExecute();
        }
    }

    /// <summary>Gets the caption of the choice of the unread notifications, with their number.</summary>
    public string UnreadFilter => UnreadCount > 0
        ? AppTexts.Format("Notifications.Unread", UnreadCount)
        : AppTexts.Get("Notifications.UnreadNone");

    /// <summary>Gets a value indicating whether only the unread notifications are shown.</summary>
    public bool ShowsUnreadOnly
    {
        get;
        private set
        {
            if (!SetProperty(ref field, value))
                return;

            OnPropertyChanged(nameof(ShowsAll));
            OnPropertyChanged(nameof(EmptyText));
            Show();
        }
    }

    /// <summary>Gets a value indicating whether every notification is shown.</summary>
    public bool ShowsAll => !ShowsUnreadOnly;

    /// <summary>Gets that the notifications are from the device or could not be read, or <see langword="null"/>.</summary>
    public string? Notice
    {
        get;
        private set
        {
            if (SetProperty(ref field, value))
                OnPropertyChanged(nameof(IsEmpty));
        }
    }

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

    /// <summary>Gets the command that shows every notification.</summary>
    public Command ShowAllCommand { get; }

    /// <summary>Gets the command that shows only the unread notifications.</summary>
    public Command ShowUnreadCommand { get; }

    /// <summary>Reads the notifications.</summary>
    /// <returns>A task that completes when the page shows them.</returns>
    public async Task LoadAsync()
    {
        try
        {
            var notifications = await _notifications.ReadAsync(CancellationToken.None);
            _read = notifications.Value;
            Notice = notifications.IsFromCache
                ? AppTexts.Format(
                    "Notifications.Offline", notifications.SavedAt.ToLocalTime().ToString("g", CultureInfo.CurrentCulture))
                : null;
        }
        catch (Exception exception) when (exception is ApiException or HttpRequestException)
        {
            _read = [];
            Notice = AppTexts.Get("Notifications.Unavailable");
        }

        UnreadCount = _read.Count(item => item.ReadAtUtc is null);
        Show();
    }

    // The icon and the tone of a kind of notification.
    private static (string Icon, StatusTone Tone) LookOf(NotificationType? type) => type switch
    {
        NotificationType.DrivingLessonBooked or NotificationType.RateDrivingLesson => ("car", StatusTone.Accent),
        NotificationType.DrivingBookingCancelled => ("circle-x", StatusTone.Danger),
        NotificationType.LowDrivingLessonRating => ("triangle-alert", StatusTone.Warning),
        NotificationType.TheoryLessonChanged => ("book-open", StatusTone.Neutral),
        NotificationType.LessonReminder => ("clock", StatusTone.Accent),
        NotificationType.EnrolledInGroup => ("users", StatusTone.Neutral),
        NotificationType.ExamChanged or NotificationType.ExamReminder => ("clipboard-check", StatusTone.Warning),
        NotificationType.ExamResult => ("clipboard-check", StatusTone.Success),
        NotificationType.PaymentConfirmed => ("wallet", StatusTone.Success),
        NotificationType.PaymentRefunded => ("wallet", StatusTone.Warning),
        NotificationType.NewMessage => ("message-circle", StatusTone.Neutral),
        NotificationType.TaskAssigned => ("check", StatusTone.Accent),
        NotificationType.TaskOverdue => ("triangle-alert", StatusTone.Danger),
        NotificationType.NewLead or NotificationType.LeadAssigned => ("inbox", StatusTone.Accent),
        _ => ("bell", StatusTone.Neutral)
    };

    // Opens what a kind of notification is about, if the application has a place for it.
    private static Task OpenSubjectAsync(NotificationType? type) => type switch
    {
        NotificationType.NewMessage => Routes.GoToAsync(Routes.Main + "/chats"),
        NotificationType.DrivingLessonBooked or NotificationType.DrivingBookingCancelled or NotificationType.RateDrivingLesson
            or NotificationType.TheoryLessonChanged or NotificationType.LessonReminder or NotificationType.EnrolledInGroup
            or NotificationType.ExamChanged or NotificationType.ExamReminder => Routes.GoToAsync(Routes.Schedule),
        NotificationType.ExamResult => Routes.GoToAsync(Routes.Main + "/study"),
        NotificationType.PaymentConfirmed or NotificationType.PaymentRefunded => Routes.OpenAsync(Routes.Contracts),
        NotificationType.TaskAssigned or NotificationType.TaskOverdue => Routes.OpenAsync(Routes.Tasks),
        NotificationType.NewLead or NotificationType.LeadAssigned => Routes.GoToAsync(Routes.Leads),
        _ => Task.CompletedTask
    };

    // The notifications shown, by the days they came on.
    private void Show()
    {
        var today = DateOnly.FromDateTime(_timeProvider.GetLocalNow().DateTime);
        Groups =
        [
            .. _read
                .Where(item => !ShowsUnreadOnly || item.ReadAtUtc is null)
                .Select(item => (Item: item, Entry: Entry(item)))
                .Where(pair => pair.Entry is not null && pair.Item.CreatedAtUtc is not null)
                .GroupBy(pair => DateOnly.FromDateTime(pair.Item.CreatedAtUtc!.Value.ToLocalTime().DateTime))
                .OrderByDescending(group => group.Key)
                .Select(group => new NotificationGroup { Title = DayOf(group.Key, today), Items = [.. group.Select(pair => pair.Entry!)] })
        ];
    }

    private static string DayOf(DateOnly day, DateOnly today) => (today.DayNumber - day.DayNumber) switch
    {
        0 => AppTexts.Get("Notifications.Today"),
        1 => AppTexts.Get("Notifications.Yesterday"),
        _ => day.ToString("d MMMM", CultureInfo.CurrentCulture)
    };

    // How long ago a notification came today, or the day and the time of an earlier one.
    private string TimeOf(DateTimeOffset createdAt)
    {
        var now = _timeProvider.GetLocalNow();
        var local = createdAt.ToLocalTime();
        var culture = CultureInfo.CurrentCulture;
        var ago = now - local;
        var days = (now.Date - local.Date).Days;

        return days switch
        {
            0 when ago < TimeSpan.FromMinutes(1) => AppTexts.Get("Notifications.JustNow"),
            0 when ago < TimeSpan.FromHours(1) => AppTexts.Format("Notifications.MinutesAgo", (int)ago.TotalMinutes),
            0 => AppTexts.Format("Notifications.HoursAgo", (int)ago.TotalHours),
            1 => AppTexts.Format("Notifications.YesterdayAt", local.ToString("t", culture)),
            _ => local.ToString("g", culture)
        };
    }

    private NotificationEntry? Entry(NotificationItem item)
    {
        if (item is not { Id: { } id, CreatedAtUtc: { } createdAt })
            return null;

        var (icon, tone) = LookOf(item.Type);
        var isUnread = item.ReadAtUtc is null;

        return new NotificationEntry
        {
            Title = item.Title ?? string.Empty,
            Body = item.Body,
            Time = TimeOf(createdAt),
            Icon = icon,
            Tone = tone,
            IsUnread = isUnread,
            OpenCommand = new Command(async () => await OpenAsync(id, isUnread, item.Type))
        };
    }

    private async Task OpenAsync(Guid notificationId, bool isUnread, NotificationType? type)
    {
        if (isUnread)
        {
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

        await OpenSubjectAsync(type);
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
