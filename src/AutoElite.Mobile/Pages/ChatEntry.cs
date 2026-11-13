using System.Windows.Input;

namespace AutoElite.Mobile.Pages;

/// <summary>A chat on the chats page.</summary>
public sealed class ChatEntry
{
    /// <summary>Gets the title: the other user of a direct chat, or the name of a group chat.</summary>
    public required string Title { get; init; }

    /// <summary>Gets the latest message, if any.</summary>
    public string? LastMessage { get; init; }

    /// <summary>Gets when the latest message was sent, in the time zone of the device.</summary>
    public string? Time { get; init; }

    /// <summary>Gets the number of unread messages, or <see langword="null"/> when all are read.</summary>
    public string? Unread { get; init; }

    /// <summary>Gets a value indicating whether some messages are unread.</summary>
    public bool HasUnread => Unread is not null;

    /// <summary>Gets the command that opens the chat.</summary>
    public required ICommand OpenCommand { get; init; }
}
