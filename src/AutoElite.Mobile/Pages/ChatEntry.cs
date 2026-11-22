using System.Windows.Input;

namespace AutoElite.Mobile.Pages;

/// <summary>A chat on the chats page.</summary>
public sealed class ChatEntry
{
    /// <summary>Gets the title: the other user of a direct chat, or the name of a group chat.</summary>
    public required string Title { get; init; }

    /// <summary>Gets a value indicating whether the chat is a group chat, shown by an icon rather than initials.</summary>
    public bool IsGroup { get; init; }

    /// <summary>Gets a value indicating whether the chat is with one person, shown by their initials.</summary>
    public bool IsPersonal => !IsGroup;

    /// <summary>Gets the latest message, if any.</summary>
    public string? LastMessage { get; init; }

    /// <summary>Gets when the latest message was sent, in the time zone of the device, and whether the chat is muted.</summary>
    public string? Time { get; init; }

    /// <summary>Gets the number of unread messages.</summary>
    public int UnreadCount { get; init; }

    /// <summary>Gets a value indicating whether some messages are unread.</summary>
    public bool HasUnread => UnreadCount > 0;

    /// <summary>Gets a value indicating whether the chat is the one open in the pane of a wide window.</summary>
    public bool IsSelected { get; init; }

    /// <summary>Gets the command that opens the chat.</summary>
    public required ICommand OpenCommand { get; init; }
}
