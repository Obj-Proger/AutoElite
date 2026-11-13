using System.Globalization;
using AutoElite.Client.Api;
using AutoElite.Client.Api.Models;
using AutoElite.Client.Offline;

namespace AutoElite.Mobile.Schedule;

/// <summary>
/// The lessons of the signed-in instructor from a day ago to two weeks ahead, read from the API or, without a network,
/// from the device: the practice slots of a driving instructor with their bookings, the theory lessons of a theory
/// instructor with the students expected at a lesson.
/// </summary>
/// <param name="cache">What the user last read.</param>
/// <param name="createApi">Creates the client of the API.</param>
/// <param name="timeProvider">The clock.</param>
public sealed class InstructorSchedule(OfflineCache cache, Func<AutoEliteApiClient> createApi, TimeProvider timeProvider)
{
    private static readonly TimeSpan _past = TimeSpan.FromDays(1);
    private static readonly TimeSpan _ahead = TimeSpan.FromDays(15);

    /// <summary>Reads the slots, the earliest first; the API gives an instructor only their own.</summary>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    /// <returns>The slots with their bookings.</returns>
    public Task<Cached<IReadOnlyList<DrivingScheduleItem>>> ReadDrivingAsync(CancellationToken cancellationToken)
    {
        var fromUtc = timeProvider.GetUtcNow() - _past;
        var toUtc = fromUtc + _ahead;

        return cache.GetListAsync(
            "driving-slots/mine",
            token => createApi().Api.V1.DrivingSlots.GetAsync(
                request =>
                {
                    request.QueryParameters.FromUtc = fromUtc;
                    request.QueryParameters.ToUtc = toUtc;
                },
                token),
            DrivingScheduleItem.CreateFromDiscriminatorValue,
            cancellationToken);
    }

    /// <summary>
    /// Reads the theory lessons, the earliest first; the API gives an instructor the lessons they give and those of
    /// the groups they are responsible for.
    /// </summary>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    /// <returns>The lessons.</returns>
    public Task<Cached<IReadOnlyList<TheoryLessonItem>>> ReadTheoryAsync(CancellationToken cancellationToken)
    {
        var fromUtc = timeProvider.GetUtcNow() - _past;
        var toUtc = fromUtc + _ahead;

        return cache.GetListAsync(
            "theory-lessons/mine",
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
}
