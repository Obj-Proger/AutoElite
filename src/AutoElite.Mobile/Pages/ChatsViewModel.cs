using System.Globalization;
using AutoElite.Client.Api.Models;
using AutoElite.Client.Realtime;
using AutoElite.Mobile.Localization;
using AutoElite.Mobile.Schedule;
using Microsoft.Kiota.Abstractions;

namespace AutoElite.Mobile.Pages;

/// <summary>
/// The chats of the user, the one with the latest message first, each with its latest message, the number of unread
/// ones and whether its notifications are off, from the API or, without a network, from the device, narrowed by a
/// search; a new message reads them again at once. A chat opens on a page of its own or, on a wide window, in the pane
/// next to the list. A new personal chat starts from the people the user studies or teaches with.
/// </summary>
public sealed class ChatsViewModel : ViewModel
{
    private readonly Chats _chats;
    private readonly RealtimeConnection _realtime;
    private IReadOnlyList<ChatListItem> _read = [];
    private Guid? _selectedId;

    /// <summary>Initializes a new instance of the <see cref="ChatsViewModel"/> class.</summary>
    /// <param name="chats">The chats.</param>
    /// <param name="realtime">The connection through which new messages arrive at once.</param>
    /// <param name="conversation">The chat open in the pane of a wide window.</param>
    public ChatsViewModel(Chats chats, RealtimeConnection realtime, ChatViewModel conversation)
    {
        ArgumentNullException.ThrowIfNull(conversation);
        _chats = chats;
        _realtime = realtime;
        Conversation = conversation;
        Conversation.Left += async (_, _) =>
        {
            Select(null);
            await LoadAsync();
        };
        RefreshCommand = new Command(async () => await RefreshAsync());
        NewChatCommand = new Command(async () => await Routes.OpenAsync(Routes.NewChat));
    }

    /// <summary>Gets the chats that match the search.</summary>
    public IReadOnlyList<ChatEntry> Items
    {
        get;
        private set => SetProperty(ref field, value);
    } = [];

    /// <summary>Gets or sets the search: a part of the title or of the latest message of a chat.</summary>
    public string Query
    {
        get;
        set
        {
            if (SetProperty(ref field, value))
                Show();
        }
    } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether the window is wide enough for the chat next to the list; the page sets
    /// it from its width.
    /// </summary>
    public bool IsWide
    {
        get;
        set
        {
            if (!SetProperty(ref field, value))
                return;

            OnPropertyChanged(nameof(ChoosesChat));
            Show();
        }
    }

    /// <summary>Gets the chat open in the pane of a wide window.</summary>
    public ChatViewModel Conversation { get; }

    /// <summary>Gets a value indicating whether a chat is open in the pane.</summary>
    public bool HasConversation => _selectedId is not null;

    /// <summary>Gets a value indicating whether the pane asks to choose a chat, as none is open.</summary>
    public bool ChoosesChat => IsWide && !HasConversation;
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

    /// <summary>Gets the command that opens the people to start a personal chat with.</summary>
    public Command NewChatCommand { get; }

    /// <summary>Starts reading the chats, and the chat in the pane, again whenever a message arrives, while the page is shown.</summary>
    public void StartListening()
    {
        _realtime.ChatMessageReceived += OnMessageReceived;
        Conversation.StartListening();
    }

    /// <summary>Stops reading the chats again when a message arrives, once the page is no longer shown.</summary>
    public void StopListening()
    {
        _realtime.ChatMessageReceived -= OnMessageReceived;
        Conversation.StopListening();
    }

    /// <summary>Reads the chats.</summary>
    /// <returns>A task that completes when the page shows them.</returns>
    public async Task LoadAsync()
    {
        try
        {
            var chats = await _chats.ReadChatsAsync(CancellationToken.None);
            _read = chats.Value;
            if (_selectedId is { } selectedId && !_read.Any(chat => chat.Id == selectedId))
                Select(null);

            Show();
            Notice = chats.IsFromCache
                ? AppTexts.Format("Chats.Offline", chats.SavedAt.ToLocalTime().ToString("g", CultureInfo.CurrentCulture))
                : _read.Count == 0 ? AppTexts.Get("Chats.Empty") : null;
        }
        catch (Exception exception) when (exception is ApiException or HttpRequestException)
        {
            _read = [];
            Show();
            Notice = AppTexts.Get("Chats.Unavailable");
        }
    }

    private void OnMessageReceived(object? sender, ChatMessageSignal signal) =>
        MainThread.BeginInvokeOnMainThread(async () => await LoadAsync());

    // When the latest message was sent: its time today, yesterday, the day of the week within a week, else its date.
    private static string? TimeOf(DateTimeOffset? sentAt)
    {
        if (sentAt?.ToLocalTime() is not { } local)
            return null;

        var culture = CultureInfo.CurrentCulture;
        var days = (DateTime.Today - local.Date).Days;

        return days switch
        {
            0 => local.ToString("t", culture),
            1 => AppTexts.Get("Chats.Yesterday"),
            < 7 => local.ToString("ddd", culture),
            _ => local.ToString("d MMM", culture)
        };
    }

    // The chats that match the search, the open one marked.
    private void Show()
    {
        var query = Query.Trim();
        Items =
        [
            .. _read
                .Where(chat => query.Length == 0
                    || (chat.Title?.Contains(query, StringComparison.CurrentCultureIgnoreCase) ?? false)
                    || (chat.LastMessageText?.Contains(query, StringComparison.CurrentCultureIgnoreCase) ?? false))
                .Select(Entry)
                .OfType<ChatEntry>()
        ];
    }

    private ChatEntry? Entry(ChatListItem chat)
    {
        if (chat is not { Id: { } chatId })
            return null;

        var title = chat.Title ?? string.Empty;
        var request = new ChatRequest(chatId, title, chat.Kind == ChatKind.Direct);

        return new ChatEntry
        {
            Title = title,
            IsGroup = chat.Kind == ChatKind.Group,
            LastMessage = chat.LastMessageText,
            Time = string.Join(
                " · ",
                new[] { TimeOf(chat.LastMessageAtUtc), chat.IsMuted == true ? AppTexts.Get("Chats.Muted") : null }.OfType<string>()),
            UnreadCount = chat.UnreadCount ?? 0,
            IsSelected = IsWide && chatId == _selectedId,
            OpenCommand = new Command(async () => await OpenAsync(request))
        };
    }

    // A chat on its page or, on a wide window, in the pane; reading it there marks its messages read in the list too.
    private async Task OpenAsync(ChatRequest request)
    {
        if (!IsWide)
        {
            await Routes.OpenAsync(Routes.Chat, new Dictionary<string, object> { [ChatViewModel.ChatParameter] = request });

            return;
        }

        // The header of the pane has room for the full name of the person, so it is not shortened.
        Conversation.Open(request with { IsPersonal = false });
        Select(request.ChatId);
        Show();
        await Conversation.LoadChatAsync();
        await Conversation.LoadAsync();
        await LoadAsync();
    }

    private void Select(Guid? chatId)
    {
        _selectedId = chatId;
        OnPropertyChanged(nameof(HasConversation));
        OnPropertyChanged(nameof(ChoosesChat));
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
