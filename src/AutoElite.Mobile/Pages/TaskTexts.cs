using System.Globalization;
using AutoElite.Client.Api.Models;
using AutoElite.Mobile.Localization;

namespace AutoElite.Mobile.Pages;

/// <summary>How a task of the staff reads in the application.</summary>
internal static class TaskTexts
{
    /// <summary>Gives the status of a task, and that it is overdue.</summary>
    /// <param name="status">The status.</param>
    /// <param name="dueAtUtc">The deadline.</param>
    /// <param name="now">The present moment.</param>
    /// <returns>The status as the user reads it.</returns>
    public static string StatusOf(StaffTaskStatus? status, DateTimeOffset? dueAtUtc, DateTimeOffset now) =>
        LessonTexts.DetailsOf(
            status switch
            {
                StaffTaskStatus.Done => AppTexts.Get("Task.Done"),
                StaffTaskStatus.InProgress => AppTexts.Get("Task.InProgress"),
                _ => AppTexts.Get("Task.New")
            },
            status != StaffTaskStatus.Done && dueAtUtc < now ? AppTexts.Get("Task.Overdue") : null);

    /// <summary>Gives the deadline and how often the task comes back.</summary>
    /// <param name="dueAtUtc">The deadline.</param>
    /// <param name="recurrence">How often the task comes back.</param>
    /// <returns>The deadline as the user reads it.</returns>
    public static string DueOf(DateTimeOffset? dueAtUtc, TaskRecurrence? recurrence) =>
        LessonTexts.DetailsOf(
            dueAtUtc is { } due ? AppTexts.Format("Task.Due", due.ToLocalTime().ToString("g", CultureInfo.CurrentCulture)) : null,
            recurrence switch
            {
                TaskRecurrence.Daily => AppTexts.Get("Task.Daily"),
                TaskRecurrence.Weekly => AppTexts.Get("Task.Weekly"),
                TaskRecurrence.Monthly => AppTexts.Get("Task.Monthly"),
                _ => null
            });
}
