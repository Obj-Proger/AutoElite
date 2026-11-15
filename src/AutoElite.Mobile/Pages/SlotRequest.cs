namespace AutoElite.Mobile.Pages;

/// <summary>The time a new practice slot starts at, chosen on the schedule, passed to the page that offers it.</summary>
/// <param name="Start">The start, in the time zone of the device.</param>
public sealed record SlotRequest(DateTimeOffset Start);
