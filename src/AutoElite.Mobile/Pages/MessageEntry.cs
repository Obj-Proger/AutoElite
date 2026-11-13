namespace AutoElite.Mobile.Pages;

/// <summary>A message in a chat, the user's own on the right.</summary>
public sealed class MessageEntry
{
    /// <summary>Gets who sent the message; empty for the user's own.</summary>
    public required string Sender { get; init; }

    /// <summary>Gets the text, or that the message was deleted or carries files only.</summary>
    public required string Text { get; init; }

    /// <summary>Gets when the message was sent, or that it waits to be sent.</summary>
    public required string Time { get; init; }

    /// <summary>Gets a value indicating whether the user sent the message.</summary>
    public bool IsMine { get; init; }

    /// <summary>Gets a value indicating whether someone else sent the message.</summary>
    public bool IsTheirs => !IsMine;

    /// <summary>Gets where the message stands: on the right for the user's own.</summary>
    public LayoutOptions Alignment => IsMine ? LayoutOptions.End : LayoutOptions.Start;
}
