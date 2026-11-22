namespace AutoElite.Mobile.Pages;

/// <summary>The notifications of a day, on the notifications page: today, yesterday or a date.</summary>
public sealed class NotificationGroup
{
    /// <summary>Gets the day.</summary>
    public required string Title { get; init; }

    /// <summary>Gets the notifications of the day, the latest first.</summary>
    public required IReadOnlyList<NotificationEntry> Items { get; init; }
}
