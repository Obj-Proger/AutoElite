using System.Globalization;
using AutoElite.Client.Api;
using AutoElite.Client.Api.Models;
using AutoElite.Client.Offline;

namespace AutoElite.Mobile.Schedule;

/// <summary>
/// What an instructor reads about a student they teach: the progress of each contract, without its money, and the
/// driving lessons the student had or has with the driving instructor, read from the API or, without a network, from
/// the device.
/// </summary>
/// <param name="cache">What the user last read.</param>
/// <param name="createApi">Creates the client of the API.</param>
/// <param name="timeProvider">The clock.</param>
public sealed class StudentProgressBook(OfflineCache cache, Func<AutoEliteApiClient> createApi, TimeProvider timeProvider)
{
    // The lessons of the last weeks and of the next two, within the 92 days the API reads at once.
    private static readonly TimeSpan _past = TimeSpan.FromDays(77);
    private static readonly TimeSpan _ahead = TimeSpan.FromDays(14);

    /// <summary>Reads the progress of a student.</summary>
    /// <param name="studentId">The student.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    /// <returns>The progress.</returns>
    public Task<Cached<StudentProgress>> ReadProgressAsync(Guid studentId, CancellationToken cancellationToken) =>
        cache.GetAsync(
            string.Create(CultureInfo.InvariantCulture, $"students/{studentId:N}/progress"),
            token => createApi().Api.V1.Students[studentId].Progress.GetAsync(cancellationToken: token),
            StudentProgress.CreateFromDiscriminatorValue,
            cancellationToken);

    /// <summary>Reads the driving lessons of a student with the signed-in driving instructor, the latest first.</summary>
    /// <param name="studentId">The student.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    /// <returns>The slots with the bookings of the student, not cancelled.</returns>
    public async Task<Cached<IReadOnlyList<DrivingScheduleItem>>> ReadLessonsAsync(Guid studentId, CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        var slots = await cache.GetListAsync(
            "driving-slots/mine/recent",
            token => createApi().Api.V1.DrivingSlots.GetAsync(
                request =>
                {
                    request.QueryParameters.FromUtc = now - _past;
                    request.QueryParameters.ToUtc = now + _ahead;
                },
                token),
            DrivingScheduleItem.CreateFromDiscriminatorValue,
            cancellationToken);
        IReadOnlyList<DrivingScheduleItem> lessons =
        [
            .. slots.Value
                .Where(item => item.Booking is { } booking
                    && booking.StudentId == studentId
                    && booking.Status != DrivingBookingStatus.Cancelled)
                .OrderByDescending(item => item.Slot?.StartUtc)
        ];

        return new Cached<IReadOnlyList<DrivingScheduleItem>>(lessons, slots.SavedAt, slots.IsFromCache);
    }
}
