namespace AutoElite.Application.Notifications.SendReminders;

/// <summary>
/// Reminds the participants of upcoming lessons and exams, as the school settings configure (FR-10.3): the student and
/// the instructor of a booked driving lesson, the current students and the instructor of a scheduled theory lesson,
/// the current students and the responsible instructor of the group of a scheduled exam.
/// </summary>
/// <remarks>
/// A scheduled job sends it every few minutes; running it again sends no reminder twice. Runs must not overlap.
/// </remarks>
[SystemRequest]
public sealed record SendRemindersCommand : ICommand;
