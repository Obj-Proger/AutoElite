using System.Windows.Input;

namespace AutoElite.Mobile.Pages;

/// <summary>A message in a chat, the user's own on the right.</summary>
public sealed class MessageEntry
{
    /// <summary>Gets who sent the message; empty for the user's own.</summary>
    public required string Sender { get; init; }

    /// <summary>Gets the text, or that the message was deleted; empty for a message of files only.</summary>
    public required string Text { get; init; }

    /// <summary>Gets when the message was sent, or that it waits to be sent.</summary>
    public required string Time { get; init; }

    /// <summary>Gets a value indicating whether the user sent the message.</summary>
    public bool IsMine { get; init; }

    /// <summary>Gets a value indicating whether someone else sent the message.</summary>
    public bool IsTheirs => !IsMine;

    /// <summary>Gets a value indicating whether the message has a text to show.</summary>
    public bool HasText => Text.Length > 0;

    /// <summary>Gets the files of the message.</summary>
    public IReadOnlyList<FileEntry> Files { get; init; } = [];

    /// <summary>Gets a value indicating whether the message has files.</summary>
    public bool HasFiles => Files.Count > 0;

    /// <summary>Gets where the message stands: on the right for the user's own.</summary>
    public LayoutOptions Alignment => IsMine ? LayoutOptions.End : LayoutOptions.Start;

    /// <summary>Gets the command that offers what the user can do with the message, such as deleting their own.</summary>
    public ICommand? TapCommand { get; init; }
}
