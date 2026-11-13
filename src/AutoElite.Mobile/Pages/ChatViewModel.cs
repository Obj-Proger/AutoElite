using System.Globalization;
using AutoElite.Client.Api.Models;
using AutoElite.Client.Offline;
using AutoElite.Client.Offline.Actions;
using AutoElite.Client.Realtime;
using AutoElite.Mobile.Localization;
using AutoElite.Mobile.Schedule;
using AutoElite.Mobile.Session;
using Microsoft.Kiota.Abstractions;

namespace AutoElite.Mobile.Pages;

/// <summary>
/// A chat: its latest messages, the earliest first and the user's own on the right, from the API or, without a
/// network, from the device; the messages the user sent that wait in the offline queue; and sending a message. A new
/// message of the chat reads it again at once, and the messages shown count as read.
/// </summary>
public sealed class ChatViewModel : ViewModel, IQueryAttributable
{
    /// <summary>The navigation parameter that carries the <see cref="ChatRequest"/>.</summary>
    public const string ChatParameter = "chat";

    private readonly Chats _chats;
    private readonly UserSession _session;
    private readonly OfflineQueue _queue;
    private readonly OfflineSynchronizer _synchronizer;
    private readonly RealtimeConnection _realtime;
    private Guid _chatId;

    /// <summary>Initializes a new instance of the <see cref="ChatViewModel"/> class.</summary>
    /// <param name="chats">The chats.</param>
    /// <param name="session">The user, whose messages stand on the right.</param>
    /// <param name="queue">The messages that wait to be sent.</param>
    /// <param name="synchronizer">Sends the messages.</param>
    /// <param name="realtime">The connection through which new messages arrive at once.</param>
    public ChatViewModel(
        Chats chats, UserSession session, OfflineQueue queue, OfflineSynchronizer synchronizer, RealtimeConnection realtime)
    {
        _chats = chats;
        _session = session;
        _queue = queue;
        _synchronizer = synchronizer;
        _realtime = realtime;
        SendCommand = new Command(async () => await SendAsync(), () => !string.IsNullOrWhiteSpace(Text));
    }

    /// <summary>Gets the title of the chat.</summary>
    public string Title
    {
        get;
        private set => SetProperty(ref field, value);
    } = string.Empty;

    /// <summary>Gets the messages, the earliest first.</summary>
    public IReadOnlyList<MessageEntry> Messages
    {
        get;
        private set => SetProperty(ref field, value);
    } = [];

    /// <summary>Gets or sets the text of the message being written.</summary>
    public string Text
    {
        get;
        set
        {
            if (SetProperty(ref field, value))
                SendCommand.ChangeCanExecute();
        }
    } = string.Empty;

    /// <summary>Gets that the messages are from the device or could not be read, or <see langword="null"/>.</summary>
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

    /// <summary>Gets the command that sends the message.</summary>
    public Command SendCommand { get; }

    /// <inheritdoc />
    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        ArgumentNullException.ThrowIfNull(query);
        if (!query.TryGetValue(ChatParameter, out var value) || value is not ChatRequest request)
            return;

        _chatId = request.ChatId;
        Title = request.Title;
        Messages = [];
        Text = string.Empty;
    }

    /// <summary>Starts reading the chat again whenever a message of it arrives, while the page is shown.</summary>
    public void StartListening() => _realtime.ChatMessageReceived += OnMessageReceived;

    /// <summary>Stops reading the chat again when a message arrives, once the page is no longer shown.</summary>
    public void StopListening() => _realtime.ChatMessageReceived -= OnMessageReceived;

    /// <summary>Reads the messages and marks them read.</summary>
    /// <returns>A task that completes when the page shows them.</returns>
    public async Task LoadAsync()
    {
        Guid? userId = null;
        try
        {
            userId = (await _session.GetProfileAsync(CancellationToken.None)).Value.Id;
        }
        catch (Exception exception) when (exception is ApiException or HttpRequestException)
        {
            // Without the profile every message stands on the left.
        }

        var waiting = (await _queue.ListAsync(CancellationToken.None))
            .Where(action => !action.IsRejected)
            .Select(action => action.Action)
            .OfType<SendChatMessage>()
            .Where(message => message.ChatId == _chatId)
            .Select(message => new MessageEntry
            {
                Sender = string.Empty,
                Text = message.Text,
                Time = AppTexts.Get("Chat.Sending"),
                IsMine = true
            });

        try
        {
            var messages = await _chats.ReadMessagesAsync(_chatId, CancellationToken.None);
            Messages = [.. messages.Value.Reverse().Select(message => Entry(message, userId)), .. waiting];
            Notice = messages.IsFromCache
                ? AppTexts.Format("Chats.Offline", messages.SavedAt.ToLocalTime().ToString("g", CultureInfo.CurrentCulture))
                : null;
            if (!messages.IsFromCache && messages.Value is [{ SentAtUtc: { } latest }, ..])
                await _chats.MarkReadAsync(_chatId, latest, CancellationToken.None);
        }
        catch (Exception exception) when (exception is ApiException or HttpRequestException)
        {
            Messages = [.. waiting];
            Notice = AppTexts.Get("Chats.Unavailable");
        }
    }

    private void OnMessageReceived(object? sender, ChatMessageSignal signal)
    {
        if (signal.ChatId == _chatId)
            MainThread.BeginInvokeOnMainThread(async () => await LoadAsync());
    }

    private static MessageEntry Entry(ChatMessageItem message, Guid? userId)
    {
        var isMine = userId is not null && message.SenderId == userId;
        var text = message switch
        {
            { IsDeleted: true } => AppTexts.Get("Chat.Deleted"),
            { Text: { Length: > 0 } written } => written,
            _ => AppTexts.Get("Chat.Attachments")
        };

        return new MessageEntry
        {
            Sender = isMine ? string.Empty : message.SenderName ?? string.Empty,
            Text = text,
            Time = message.SentAtUtc?.ToLocalTime().ToString("g", CultureInfo.CurrentCulture) ?? string.Empty,
            IsMine = isMine
        };
    }

    private async Task SendAsync()
    {
        var text = Text.Trim();
        if (text.Length == 0)
            return;

        Text = string.Empty;

        // Sent at once when the API can be reached, later otherwise; the chat shows the message waiting until then.
        await _synchronizer.SubmitAsync(new SendChatMessage(_chatId, text), CancellationToken.None);
        await LoadAsync();
    }
}
