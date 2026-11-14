using AutoElite.Client.Api;
using AutoElite.Client.Api.Models;
using AutoElite.Client.Offline;

namespace AutoElite.Mobile.Schedule;

/// <summary>
/// The lessons of the signed-in student, driving and theory, for the home page or a week of the schedule, read from the
/// API or, without a network, from the device.
/// </summary>
/// <param name="cache">What the user last read.</param>
/// <param name="createApi">Creates the client of the API.</param>
/// <param name="timeProvider">The clock.</param>
public sealed class StudentSchedule(OfflineCache cache, Func<AutoEliteApiClient> createApi, TimeProvider timeProvider)
{
    private static readonly TimeSpan _past = TimeSpan.FromDays(1);
    private static readonly TimeSpan _ahead = TimeSpan.FromDays(61);
    private const string DrivingKey = "me/driving-lessons";
    private const string TheoryKey = "me/theory-lessons";

    /// <summary>Reads the lessons from a day ago to two months ahead, for the home page.</summary>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    /// <returns>The driving lessons and the theory lessons.</returns>
    public Task<StudentLessons> ReadAsync(CancellationToken cancellationToken)
    {
        var fromUtc = timeProvider.GetUtcNow() - _past;

        return ReadAsync(DrivingKey, TheoryKey, fromUtc, fromUtc + _ahead, cancellationToken);
    }

    /// <summary>Reads the lessons of a week, for the schedule.</summary>
    /// <param name="firstDay">The first day of the week.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    /// <returns>The driving lessons and the theory lessons.</returns>
    public Task<StudentLessons> ReadWeekAsync(DateOnly firstDay, CancellationToken cancellationToken) =>
        ReadAsync(
            ScheduleWeek.KeyOf(DrivingKey, firstDay),
            ScheduleWeek.KeyOf(TheoryKey, firstDay),
            ScheduleWeek.StartOf(firstDay),
            ScheduleWeek.StartOf(firstDay.AddDays(ScheduleWeek.Days)),
            cancellationToken);

    private async Task<StudentLessons> ReadAsync(
        string drivingKey, string theoryKey, DateTimeOffset fromUtc, DateTimeOffset toUtc, CancellationToken cancellationToken)
    {
        var driving = await cache.GetListAsync(
            drivingKey,
            token => createApi().Api.V1.Me.DrivingLessons.GetAsync(
                request =>
                {
                    request.QueryParameters.FromUtc = fromUtc;
                    request.QueryParameters.ToUtc = toUtc;
                },
                token),
            StudentDrivingLessonItem.CreateFromDiscriminatorValue,
            cancellationToken);
        var theory = await cache.GetListAsync(
            theoryKey,
            token => createApi().Api.V1.Me.TheoryLessons.GetAsync(
                request =>
                {
                    request.QueryParameters.FromUtc = fromUtc;
                    request.QueryParameters.ToUtc = toUtc;
                },
                token),
            StudentTheoryLessonItem.CreateFromDiscriminatorValue,
            cancellationToken);

        return new StudentLessons(driving, theory);
    }
}
