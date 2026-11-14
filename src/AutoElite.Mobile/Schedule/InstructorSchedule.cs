using System.Globalization;
using AutoElite.Client.Api;
using AutoElite.Client.Api.Models;
using AutoElite.Client.Offline;

namespace AutoElite.Mobile.Schedule;

/// <summary>
/// The lessons of the signed-in instructor, for the home page or a week of the schedule, read from the API or, without a
/// network, from the device: the practice slots of a driving instructor with their bookings, the theory lessons of a theory
/// instructor with the students expected at a lesson.
/// </summary>
/// <param name="cache">What the user last read.</param>
/// <param name="createApi">Creates the client of the API.</param>
/// <param name="timeProvider">The clock.</param>
public sealed class InstructorSchedule(OfflineCache cache, Func<AutoEliteApiClient> createApi, TimeProvider timeProvider)
{
    private static readonly TimeSpan _past = TimeSpan.FromDays(1);
    private static readonly TimeSpan _ahead = TimeSpan.FromDays(15);
    private const string DrivingKey = "driving-slots/mine";
    private const string TheoryKey = "theory-lessons/mine";

    /// <summary>Reads the slots of the next two weeks, the earliest first; the API gives an instructor only their own.</summary>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    /// <returns>The slots with their bookings.</returns>
    public Task<Cached<IReadOnlyList<DrivingScheduleItem>>> ReadDrivingAsync(CancellationToken cancellationToken)
    {
        var fromUtc = timeProvider.GetUtcNow() - _past;

        return ReadDrivingAsync(DrivingKey, fromUtc, fromUtc + _ahead, cancellationToken);
    }

    /// <summary>Reads the slots of a week, for the schedule.</summary>
    /// <param name="firstDay">The first day of the week.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    /// <returns>The slots with their bookings.</returns>
    public Task<Cached<IReadOnlyList<DrivingScheduleItem>>> ReadDrivingWeekAsync(DateOnly firstDay, CancellationToken cancellationToken) =>
        ReadDrivingAsync(
            ScheduleWeek.KeyOf(DrivingKey, firstDay),
            ScheduleWeek.StartOf(firstDay),
            ScheduleWeek.StartOf(firstDay.AddDays(ScheduleWeek.Days)),
            cancellationToken);

    /// <summary>
    /// Reads the theory lessons of the next two weeks, the earliest first; the API gives an instructor the lessons they
    /// give and those of the groups they are responsible for.
    /// </summary>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    /// <returns>The lessons.</returns>
    public Task<Cached<IReadOnlyList<TheoryLessonItem>>> ReadTheoryAsync(CancellationToken cancellationToken)
    {
        var fromUtc = timeProvider.GetUtcNow() - _past;

        return ReadTheoryAsync(TheoryKey, fromUtc, fromUtc + _ahead, cancellationToken);
    }

    /// <summary>Reads the theory lessons of a week, for the schedule.</summary>
    /// <param name="firstDay">The first day of the week.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    /// <returns>The lessons.</returns>
    public Task<Cached<IReadOnlyList<TheoryLessonItem>>> ReadTheoryWeekAsync(DateOnly firstDay, CancellationToken cancellationToken) =>
        ReadTheoryAsync(
            ScheduleWeek.KeyOf(TheoryKey, firstDay),
            ScheduleWeek.StartOf(firstDay),
            ScheduleWeek.StartOf(firstDay.AddDays(ScheduleWeek.Days)),
            cancellationToken);

    /// <summary>Reads a theory lesson with the students expected at it and their marks.</summary>
    /// <param name="lessonId">The lesson.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    /// <returns>The lesson.</returns>
    public Task<Cached<TheoryLessonDetails>> ReadTheoryLessonAsync(Guid lessonId, CancellationToken cancellationToken) =>
        cache.GetAsync(
            string.Create(CultureInfo.InvariantCulture, $"theory-lessons/{lessonId:N}"),
            token => createApi().Api.V1.TheoryLessons[lessonId].GetAsync(cancellationToken: token),
            TheoryLessonDetails.CreateFromDiscriminatorValue,
            cancellationToken);

    private Task<Cached<IReadOnlyList<DrivingScheduleItem>>> ReadDrivingAsync(
        string key, DateTimeOffset fromUtc, DateTimeOffset toUtc, CancellationToken cancellationToken) =>
        cache.GetListAsync(
            key,
            token => createApi().Api.V1.DrivingSlots.GetAsync(
                request =>
                {
                    request.QueryParameters.FromUtc = fromUtc;
                    request.QueryParameters.ToUtc = toUtc;
                },
                token),
            DrivingScheduleItem.CreateFromDiscriminatorValue,
            cancellationToken);

    private Task<Cached<IReadOnlyList<TheoryLessonItem>>> ReadTheoryAsync(
        string key, DateTimeOffset fromUtc, DateTimeOffset toUtc, CancellationToken cancellationToken) =>
        cache.GetListAsync(
            key,
            token => createApi().Api.V1.TheoryLessons.GetAsync(
                request =>
                {
                    request.QueryParameters.FromUtc = fromUtc;
                    request.QueryParameters.ToUtc = toUtc;
                },
                token),
            TheoryLessonItem.CreateFromDiscriminatorValue,
            cancellationToken);
}
