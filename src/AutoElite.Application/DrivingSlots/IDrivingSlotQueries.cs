namespace AutoElite.Application.DrivingSlots;

/// <summary>Reads driving slots for queries.</summary>
public interface IDrivingSlotQueries
{
    /// <summary>
    /// Reads the slots that match the filter and whose vehicle is inside the scope, the earliest first, with the
    /// bookings that hold them.
    /// </summary>
    /// <param name="filter">The conditions of the schedule.</param>
    /// <param name="scope">The branches the caller may see; slots with a shared vehicle are in every scope.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    Task<IReadOnlyList<DrivingScheduleItem>> GetScheduleAsync(
        DrivingScheduleFilter filter, DataScope scope, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads the available slots that match the filter and whose vehicle is inside the scope, the earliest first.
    /// </summary>
    /// <param name="filter">The conditions of the list.</param>
    /// <param name="scope">The branch of the contract; slots with a shared vehicle are in every scope.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    Task<IReadOnlyList<DrivingSlotItem>> GetAvailableAsync(
        AvailableDrivingSlotFilter filter, DataScope scope, CancellationToken cancellationToken = default);

    /// <summary>Reads the bookings of a student in slots that overlap the period, the earliest first.</summary>
    /// <param name="studentId">The identifier of the student.</param>
    /// <param name="period">The time to read.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    Task<IReadOnlyList<StudentDrivingLessonItem>> GetOfStudentAsync(
        Guid studentId, TimeInterval period, CancellationToken cancellationToken = default);

    /// <summary>Reads the card of a slot.</summary>
    /// <param name="slotId">The identifier of the slot.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    /// <returns>The card, or <see langword="null"/> if the slot does not exist.</returns>
    Task<DrivingSlotDetails?> GetDetailsAsync(Guid slotId, CancellationToken cancellationToken = default);
}
