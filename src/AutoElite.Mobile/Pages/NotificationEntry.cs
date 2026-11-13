using System.Windows.Input;

namespace AutoElite.Mobile.Pages;

/// <summary>A notification on the notifications page; touching an unread one marks it read.</summary>
public sealed class NotificationEntry
{
    /// <summary>Gets the title, in the language of the user.</summary>
    public required string Title { get; init; }

    /// <summary>Gets the text, if any.</summary>
    public string? Body { get; init; }

    /// <summary>Gets a value indicating whether the notification has a text.</summary>
    public bool HasBody => !string.IsNullOrWhiteSpace(Body);

    /// <summary>Gets when the notification was created, in the time zone of the device.</summary>
    public required string Time { get; init; }

    /// <summary>Gets a value indicating whether the user has not read it yet.</summary>
    public bool IsUnread { get; init; }

    /// <summary>Gets the command that marks it read.</summary>
    public required ICommand ReadCommand { get; init; }
}
