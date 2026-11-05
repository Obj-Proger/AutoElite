namespace AutoElite.Application.Notifications;

/// <summary>Decides which lesson reminder is due (FR-10.3).</summary>
/// <remarks>
/// A reminder with lead time <c>L</c> becomes due at <c>start − L</c>. When several are due, only the closest to the
/// lesson is sent: a lesson booked three hours ahead with reminders a day and two hours before gets only the
/// two-hour one. A reminder whose moment passed longer than <see cref="CatchUpWindow"/> ago is dropped, so a job
/// that was down does not send stale reminders, and nothing is sent once the lesson has started.
/// </remarks>
internal static class LessonReminderSchedule
{
    /// <summary>How long after its moment a missed reminder is still sent.</summary>
    public static readonly TimeSpan CatchUpWindow = TimeSpan.FromHours(1);

    /// <summary>Finds the lead time of the reminder that is due now.</summary>
    /// <param name="leadTimes">The configured lead times.</param>
    /// <param name="startUtc">The start of the lesson.</param>
    /// <param name="utcNow">The current UTC time.</param>
    /// <returns>The lead time, or <see langword="null"/> if no reminder is due.</returns>
    public static TimeSpan? DueLeadTime(IEnumerable<TimeSpan> leadTimes, DateTime startUtc, DateTime utcNow)
    {
        if (utcNow >= startUtc)
            return null;

        var due = leadTimes.Where(leadTime => startUtc - leadTime <= utcNow).Select(leadTime => (TimeSpan?)leadTime).Min();

        return due is { } leadTime && utcNow - (startUtc - leadTime) <= CatchUpWindow ? leadTime : null;
    }
}
