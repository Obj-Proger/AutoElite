using System.Globalization;
using AutoElite.Client.Api.Models;
using AutoElite.Client.Realtime;
using AutoElite.Mobile.Localization;
using AutoElite.Mobile.Schedule;
using Microsoft.Kiota.Abstractions;

namespace AutoElite.Mobile.Pages;

/// <summary>
/// The chats of the user, the one with the latest message first, each with its latest message and the number of
/// unread ones, from the API or, without a network, from the device; a new message reads them again at once.
/// </summary>
public sealed class ChatsViewModel : ViewModel
{
    private readonly Chats _chats;
    private readonly RealtimeConnection _realtime;

    /// <summary>Initializes a new instance of the <see cref="ChatsViewModel"/> class.</summary>
    /// <param name="chats">The chats.</param>
    /// <param name="realtime">The connection through which new messages arrive at once.</param>
    public ChatsViewModel(Chats chats, RealtimeConnection realtime)
    {
        _chats = chats;
        _realtime = realtime;
        RefreshCommand = new Command(async () => await RefreshAsync());
    }

    /// <summary>Gets the chats.</summary>
    public IReadOnlyList<ChatEntry> Items
    {
        get;
        private set => SetProperty(ref field, value);
    } = [];

    /// <summary>Gets that there are no chats, or that they are from the device, or <see langword="null"/>.</summary>
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

    /// <summary>Gets the command that reads the chats again.</summary>
    public Command RefreshCommand { get; }

    /// <summary>Starts reading the chats again whenever a message arrives, while the page is shown.</summary>
    public void StartListening() => _realtime.ChatMessageReceived += OnMessageReceived;

    /// <summary>Stops reading the chats again when a message arrives, once the page is no longer shown.</summary>
    public void StopListening() => _realtime.ChatMessageReceived -= OnMessageReceived;

    /// <summary>Reads the chats.</summary>
    /// <returns>A task that completes when the page shows them.</returns>
    public async Task LoadAsync()
    {
        try
        {
            var chats = await _chats.ReadChatsAsync(CancellationToken.None);
            Items = [.. chats.Value.Select(Entry).OfType<ChatEntry>()];
            Notice = chats.IsFromCache
                ? AppTexts.Format("Chats.Offline", chats.SavedAt.ToLocalTime().ToString("g", CultureInfo.CurrentCulture))
                : Items.Count == 0 ? AppTexts.Get("Chats.Empty") : null;
        }
        catch (Exception exception) when (exception is ApiException or HttpRequestException)
        {
            Items = [];
            Notice = AppTexts.Get("Chats.Unavailable");
        }
    }

    private void OnMessageReceived(object? sender, ChatMessageSignal signal) =>
        MainThread.BeginInvokeOnMainThread(async () => await LoadAsync());

    private static ChatEntry? Entry(ChatListItem chat)
    {
        if (chat is not { Id: { } chatId })
            return null;

        var title = chat.Title ?? string.Empty;

        return new ChatEntry
        {
            Title = title,
            LastMessage = chat.LastMessageText,
            Time = chat.LastMessageAtUtc?.ToLocalTime().ToString("g", CultureInfo.CurrentCulture),
            Unread = chat.UnreadCount is { } unread and > 0 ? unread.ToString(CultureInfo.CurrentCulture) : null,
            OpenCommand = new Command(async () => await Routes.OpenAsync(
                Routes.Chat,
                new Dictionary<string, object> { [ChatViewModel.ChatParameter] = new ChatRequest(chatId, title) }))
        };
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
