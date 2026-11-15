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
/// A chat: its latest messages, the earliest first and the user's own on the right, with their files, from the API or,
/// without a network, from the device; the messages the user sent that wait in the offline queue; and sending a
/// message, with up to ten files, which need the network. A new message of the chat reads it again at once, and the
/// messages shown count as read. The user deletes their own messages, turns the notifications of the chat off and
/// leaves a group chat.
/// </summary>
public sealed class ChatViewModel : ViewModel, IQueryAttributable
{
    /// <summary>The navigation parameter that carries the <see cref="ChatRequest"/>.</summary>
    public const string ChatParameter = "chat";

    // The files one message carries at most, as the API allows.
    private const int MaxFiles = 10;

    private readonly Chats _chats;
    private readonly UserSession _session;
    private readonly OfflineQueue _queue;
    private readonly OfflineSynchronizer _synchronizer;
    private readonly RealtimeConnection _realtime;
    private readonly Dictionary<Guid, DocumentItem> _files = [];
    private readonly List<(Guid DocumentId, string Name)> _pending = [];
    private Guid _chatId;
    private bool _isGroup;
    private bool _isMuted;

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
        SendCommand = new Command(async () => await SendAsync(), () => !IsBusy && (!string.IsNullOrWhiteSpace(Text) || _pending.Count > 0));
        AttachCommand = new Command(async () => await AttachAsync(), () => !IsBusy);
        ActionsCommand = new Command(async () => await ShowActionsAsync());
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

    /// <summary>Gets the files chosen for the next message.</summary>
    public IReadOnlyList<FileEntry> Pending
    {
        get;
        private set
        {
            if (SetProperty(ref field, value))
                OnPropertyChanged(nameof(HasPending));
        }
    } = [];

    /// <summary>Gets a value indicating whether files wait for the next message.</summary>
    public bool HasPending => Pending.Count > 0;

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

    /// <summary>Gets a value indicating whether files are being attached or a message with files sent.</summary>
    public bool IsBusy
    {
        get;
        private set
        {
            if (!SetProperty(ref field, value))
                return;

            SendCommand.ChangeCanExecute();
            AttachCommand.ChangeCanExecute();
        }
    }

    /// <summary>Gets that the messages are from the device or could not be read, or how an action went.</summary>
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

    /// <summary>Gets the command that chooses files for the next message.</summary>
    public Command AttachCommand { get; }

    /// <summary>Gets the command that offers turning the notifications off or on and leaving a group chat.</summary>
    public Command ActionsCommand { get; }

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
        _files.Clear();
        _pending.Clear();
        ShowPending();
    }

    /// <summary>Starts reading the chat again whenever a message of it arrives, while the page is shown.</summary>
    public void StartListening() => _realtime.ChatMessageReceived += OnMessageReceived;

    /// <summary>Stops reading the chat again when a message arrives, once the page is no longer shown.</summary>
    public void StopListening() => _realtime.ChatMessageReceived -= OnMessageReceived;

    /// <summary>Reads whether the chat is a group chat and whether its notifications are off.</summary>
    /// <returns>A task that completes when they are known.</returns>
    public async Task LoadChatAsync()
    {
        try
        {
            var chat = (await _chats.ReadChatsAsync(CancellationToken.None)).Value.FirstOrDefault(item => item.Id == _chatId);
            _isGroup = chat?.Kind == ChatKind.Group;
            _isMuted = chat?.IsMuted == true;
        }
        catch (Exception exception) when (exception is ApiException or HttpRequestException)
        {
            // A personal chat with the notifications on until the list can be read.
        }
    }

    /// <summary>Reads the messages and their files and marks the messages read.</summary>
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
            await ReadFilesAsync(messages.Value);
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

    // Reads the names and sizes of the files of the chat when a message has one not known yet.
    private async Task ReadFilesAsync(IEnumerable<ChatMessageItem> messages)
    {
        if (!messages.SelectMany(message => message.AttachmentIds ?? []).OfType<Guid>().Any(id => !_files.ContainsKey(id)))
            return;

        try
        {
            foreach (var file in (await _chats.ReadFilesAsync(_chatId, CancellationToken.None)).Value)
            {
                if (file.Id is { } id)
                    _files[id] = file;
            }
        }
        catch (Exception exception) when (exception is ApiException or HttpRequestException)
        {
            // The files are shown without their names.
        }
    }

    private MessageEntry Entry(ChatMessageItem message, Guid? userId)
    {
        var isMine = userId is not null && message.SenderId == userId;
        var isDeleted = message.IsDeleted == true;
        IReadOnlyList<FileEntry> files = isDeleted ? [] : [.. (message.AttachmentIds ?? []).OfType<Guid>().Select(FileOf)];

        return new MessageEntry
        {
            Sender = isMine ? string.Empty : message.SenderName ?? string.Empty,
            Text = isDeleted ? AppTexts.Get("Chat.Deleted") : message.Text ?? string.Empty,
            Time = message.SentAtUtc?.ToLocalTime().ToString("g", CultureInfo.CurrentCulture) ?? string.Empty,
            IsMine = isMine,
            Files = files,
            TapCommand = isMine && !isDeleted && message.Id is { } messageId
                ? new Command(async () => await OfferDeletingAsync(messageId))
                : null
        };
    }

    private FileEntry FileOf(Guid documentId)
    {
        var name = _files.TryGetValue(documentId, out var file) && !string.IsNullOrEmpty(file.FileName)
            ? file.FileName
            : AppTexts.Get("Chat.File");

        return new FileEntry
        {
            Name = name,
            Details = file?.SizeBytes is { } size ? FileSizes.Of(size) : string.Empty,
            Command = new Command(async () => await OpenFileAsync(documentId, name))
        };
    }

    private async Task OpenFileAsync(Guid documentId, string name)
    {
        try
        {
            var path = await _chats.DownloadAsync(documentId, name, CancellationToken.None);
            await Launcher.Default.OpenAsync(new OpenFileRequest(name, new ReadOnlyFile(path)));
        }
        catch (Exception exception) when (exception is ApiException or HttpRequestException or TaskCanceledException
            or InvalidOperationException or IOException)
        {
            Notice = ProblemTexts.Of(exception);
        }
    }

    private async Task OfferDeletingAsync(Guid messageId)
    {
        var delete = AppTexts.Get("Chat.DeleteMessage");
        var choice = await Shell.Current.DisplayActionSheetAsync(
            AppTexts.Get("Chat.MessageActions"), AppTexts.Get("Common.Cancel"), delete);
        if (choice != delete)
            return;

        try
        {
            await _chats.DeleteMessageAsync(messageId, CancellationToken.None);
            await LoadAsync();
        }
        catch (Exception exception) when (exception is ApiException or HttpRequestException or TaskCanceledException)
        {
            Notice = ProblemTexts.Of(exception);
        }
    }

    private async Task ShowActionsAsync()
    {
        var mute = _isMuted ? AppTexts.Get("Chat.Unmute") : AppTexts.Get("Chat.Mute");
        var leave = AppTexts.Get("Chat.Leave");
        var choice = await Shell.Current.DisplayActionSheetAsync(
            Title, AppTexts.Get("Common.Cancel"), _isGroup ? leave : null, mute);
        try
        {
            if (choice == mute)
            {
                await _chats.SetMutedAsync(_chatId, !_isMuted, CancellationToken.None);
                _isMuted = !_isMuted;
                Notice = _isMuted ? AppTexts.Get("Chat.Muted") : AppTexts.Get("Chat.Unmuted");
            }
            else if (choice == leave && _isGroup && await Shell.Current.DisplayAlertAsync(
                         AppTexts.Get("Chat.LeaveTitle"),
                         AppTexts.Get("Chat.LeaveQuestion"),
                         leave,
                         AppTexts.Get("Common.Cancel")))
            {
                await _chats.LeaveAsync(_chatId, CancellationToken.None);
                await Shell.Current.GoToAsync("..");
            }
        }
        catch (Exception exception) when (exception is ApiException or HttpRequestException or TaskCanceledException)
        {
            Notice = ProblemTexts.Of(exception);
        }
    }

    private async Task AttachAsync()
    {
        if (_pending.Count >= MaxFiles)
        {
            Notice = AppTexts.Format("Chat.TooManyFiles", MaxFiles);

            return;
        }

        IEnumerable<FileResult?> picked;
        try
        {
            picked = await FilePicker.Default.PickMultipleAsync(new PickOptions { PickerTitle = AppTexts.Get("Chat.ChooseFiles") })
                ?? [];
        }
        catch (PermissionException)
        {
            Notice = AppTexts.Get("Chat.NoFileAccess");

            return;
        }

        var files = picked.OfType<FileResult>().ToList();
        if (files.Count > MaxFiles - _pending.Count)
        {
            Notice = AppTexts.Format("Chat.TooManyFiles", MaxFiles);
            files = files.Take(MaxFiles - _pending.Count).ToList();
        }

        IsBusy = true;
        try
        {
            foreach (var file in files)
            {
                var documentId = await _chats.AttachAsync(_chatId, file, CancellationToken.None);
                _pending.Add((documentId, file.FileName));
                ShowPending();
            }
        }
        catch (Exception exception) when (exception is ApiException or HttpRequestException or TaskCanceledException
            or InvalidOperationException or IOException)
        {
            Notice = ProblemTexts.Of(exception);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void ShowPending()
    {
        Pending =
        [
            .. _pending.Select(file => new FileEntry
            {
                Name = file.Name,
                Details = string.Empty,
                Command = new Command(async () => await RemovePendingAsync(file.DocumentId))
            })
        ];
        SendCommand.ChangeCanExecute();
    }

    private async Task RemovePendingAsync(Guid documentId)
    {
        _pending.RemoveAll(file => file.DocumentId == documentId);
        ShowPending();
        try
        {
            await _chats.DeleteFileAsync(documentId, CancellationToken.None);
        }
        catch (Exception exception) when (exception is ApiException or HttpRequestException or TaskCanceledException)
        {
            // The file stays attached to the chat, unsent and unseen; nothing for the user to do.
        }
    }

    private async Task SendAsync()
    {
        var text = Text.Trim();
        if (_pending.Count == 0)
        {
            if (text.Length == 0)
                return;

            Text = string.Empty;

            // Sent at once when the API can be reached, later otherwise; the chat shows the message waiting until then.
            await _synchronizer.SubmitAsync(new SendChatMessage(_chatId, text), CancellationToken.None);
            await LoadAsync();

            return;
        }

        IsBusy = true;
        try
        {
            await _chats.SendWithFilesAsync(_chatId, text, [.. _pending.Select(file => file.DocumentId)], CancellationToken.None);
            _pending.Clear();
            ShowPending();
            Text = string.Empty;
            await LoadAsync();
        }
        catch (Exception exception) when (exception is ApiException or HttpRequestException or TaskCanceledException)
        {
            Notice = ProblemTexts.Of(exception);
        }
        finally
        {
            IsBusy = false;
        }
    }
}
