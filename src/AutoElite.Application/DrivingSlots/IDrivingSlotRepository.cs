namespace AutoElite.Application.DrivingSlots;

/// <summary>Loads and adds driving slots for commands; a loaded slot includes its bookings.</summary>
public interface IDrivingSlotRepository : IRepository<DrivingSlot>
{
    /// <summary>
    /// Determines whether another slot that is not withdrawn overlaps the time for the instructor or for the vehicle,
    /// since neither can be in two lessons at once.
    /// </summary>
    /// <param name="instructorId">The instructor of the slot.</param>
    /// <param name="vehicleId">The vehicle of the slot.</param>
    /// <param name="time">The time of the slot.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    Task<bool> HasOverlapAsync(Guid instructorId, Guid vehicleId, TimeInterval time, CancellationToken cancellationToken = default);

    /// <summary>
    /// Determines whether the student has a booking that is not cancelled in another slot, not withdrawn, that
    /// overlaps the time.
    /// </summary>
    /// <param name="studentId">The student.</param>
    /// <param name="time">The time of the slot being booked.</param>
    /// <param name="exceptSlotId">The slot being booked, left out.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    Task<bool> StudentHasBookingAtAsync(
        Guid studentId, TimeInterval time, Guid exceptSlotId, CancellationToken cancellationToken = default);

    /// <summary>Loads the slots in which the contract has an active booking for a lesson that starts after the time.</summary>
    /// <param name="contractId">The identifier of the contract.</param>
    /// <param name="afterUtc">Only lessons that start later than this UTC time.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    Task<IReadOnlyList<DrivingSlot>> GetBookedForContractAfterAsync(
        Guid contractId, DateTime afterUtc, CancellationToken cancellationToken = default);

    /// <summary>Loads the booked slots whose lessons start after one UTC time and no later than another.</summary>
    /// <param name="afterUtc">Only lessons that start later than this UTC time.</param>
    /// <param name="untilUtc">Only lessons that start no later than this UTC time.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    Task<IReadOnlyList<DrivingSlot>> GetBookedStartingBetweenAsync(
        DateTime afterUtc, DateTime untilUtc, CancellationToken cancellationToken = default);
}
