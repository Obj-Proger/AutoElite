using System.Globalization;
using AutoElite.Client.Api;
using AutoElite.Client.Api.Models;
using AutoElite.Client.Offline;

namespace AutoElite.Mobile.Schedule;

/// <summary>
/// What an instructor reads about the students they teach: the students a driving instructor had or has lessons with
/// in the last weeks and the next two, and about one of them the progress of each contract, without its money, and the
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

    /// <summary>
    /// Reads the students of the signed-in driving instructor: those with a lesson with them, not cancelled, in the last
    /// weeks or the next two; those with a lesson ahead first, by the nearest one, then the others by their last one.
    /// </summary>
    /// <param name="instructorId">The instructor, whose slots alone count when they are also staff of the office.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    /// <returns>The students with their lessons.</returns>
    public async Task<Cached<IReadOnlyList<DrivingStudent>>> ReadStudentsAsync(Guid instructorId, CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        var slots = await ReadRecentAsync(cancellationToken);
        IReadOnlyList<DrivingStudent> students =
        [
            .. slots.Value
                .Where(item => item is { Slot.StartUtc: not null, Booking: { StudentId: not null } booking }
                    && item.Slot.InstructorId == instructorId
                    && booking.Status != DrivingBookingStatus.Cancelled)
                .GroupBy(item => item.Booking!.StudentId!.Value)
                .Select(lessons => new DrivingStudent(
                    lessons.Key,
                    lessons.Select(item => item.Booking!.StudentName).FirstOrDefault(name => name is not null) ?? string.Empty,
                    lessons.Count(item => item.Booking!.Status == DrivingBookingStatus.Completed),
                    lessons.Select(item => item.Slot!.StartUtc!.Value).Where(start => start > now).Order().Cast<DateTimeOffset?>()
                        .FirstOrDefault(),
                    lessons.Select(item => item.Slot!.StartUtc!.Value).Where(start => start <= now).Order().Cast<DateTimeOffset?>()
                        .LastOrDefault()))
                .OrderBy(student => student.Next is null)
                .ThenBy(student => student.Next)
                .ThenByDescending(student => student.Last)
        ];

        return new Cached<IReadOnlyList<DrivingStudent>>(students, slots.SavedAt, slots.IsFromCache);
    }

    /// <summary>Reads the driving lessons of a student with the signed-in driving instructor, the latest first.</summary>
    /// <param name="studentId">The student.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    /// <returns>The slots with the bookings of the student, not cancelled.</returns>
    public async Task<Cached<IReadOnlyList<DrivingScheduleItem>>> ReadLessonsAsync(Guid studentId, CancellationToken cancellationToken)
    {
        var slots = await ReadRecentAsync(cancellationToken);
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

    // The slots of the instructor in the last weeks and the next two, which both the students and a student's lessons
    // come from; the API gives a driving instructor only their own slots.
    private Task<Cached<IReadOnlyList<DrivingScheduleItem>>> ReadRecentAsync(CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();

        return cache.GetListAsync(
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
    }
}
