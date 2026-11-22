using System.Windows.Input;
using AutoElite.Mobile.Controls;

namespace AutoElite.Mobile.Pages;

/// <summary>
/// A notification on the notifications page; touching it marks it read and opens what it is about, such as the
/// schedule for a lesson or the chats for a message.
/// </summary>
public sealed class NotificationEntry
{
    /// <summary>Gets the title, in the language of the user.</summary>
    public required string Title { get; init; }

    /// <summary>Gets the text, if any.</summary>
    public string? Body { get; init; }

    /// <summary>Gets a value indicating whether the notification has a text.</summary>
    public bool HasBody => !string.IsNullOrWhiteSpace(Body);

    /// <summary>Gets when the notification was created: how long ago today, else the day and the time.</summary>
    public required string Time { get; init; }

    /// <summary>Gets the icon of what the notification is about.</summary>
    public required string Icon { get; init; }

    /// <summary>Gets what the notification tells, which gives the color of its icon.</summary>
    public StatusTone Tone { get; init; }

    /// <summary>Gets a value indicating whether the user has not read it yet.</summary>
    public bool IsUnread { get; init; }

    /// <summary>Gets the command that marks it read and opens what it is about.</summary>
    public required ICommand OpenCommand { get; init; }
}
