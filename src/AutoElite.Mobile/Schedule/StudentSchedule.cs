using AutoElite.Client.Api;
using AutoElite.Client.Api.Models;
using AutoElite.Client.Offline;

namespace AutoElite.Mobile.Schedule;

/// <summary>
/// The lessons of the signed-in student, driving and theory, from a day ago to two months ahead, read from the API or,
/// without a network, from the device.
/// </summary>
/// <param name="cache">What the user last read.</param>
/// <param name="createApi">Creates the client of the API.</param>
/// <param name="timeProvider">The clock.</param>
public sealed class StudentSchedule(OfflineCache cache, Func<AutoEliteApiClient> createApi, TimeProvider timeProvider)
{
    private static readonly TimeSpan _past = TimeSpan.FromDays(1);
    private static readonly TimeSpan _ahead = TimeSpan.FromDays(61);

    /// <summary>Reads the lessons.</summary>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    /// <returns>The driving lessons and the theory lessons.</returns>
    public async Task<StudentLessons> ReadAsync(CancellationToken cancellationToken)
    {
        var fromUtc = timeProvider.GetUtcNow() - _past;
        var toUtc = fromUtc + _ahead;

        var driving = await cache.GetListAsync(
            "me/driving-lessons",
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
            "me/theory-lessons",
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
