namespace AutoElite.Mobile.Pages;

/// <summary>The lesson the completion page completes, passed to it when it is opened.</summary>
/// <param name="SlotId">The slot of the lesson.</param>
/// <param name="PlannedHours">The planned length of the lesson, offered as the hours it lasted.</param>
/// <param name="Student">The full name of the student.</param>
/// <param name="Time">The day and the time of the lesson.</param>
public sealed record CompletionRequest(Guid SlotId, decimal PlannedHours, string Student, string Time);
