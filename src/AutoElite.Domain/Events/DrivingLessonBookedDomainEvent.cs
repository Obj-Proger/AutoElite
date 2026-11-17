namespace AutoElite.Domain.Events;

/// <summary>
/// Raised when a driving slot is booked, by the student or by a staff member for them; the instructor is notified and
/// a reminder is scheduled, and a student booked by a staff member is told.
/// </summary>
/// <param name="SlotId">The identifier of the slot.</param>
/// <param name="BookingId">The identifier of the booking.</param>
/// <param name="InstructorId">The identifier of the driving instructor.</param>
/// <param name="StudentId">The identifier of the student.</param>
/// <param name="StartUtc">The UTC start of the lesson.</param>
/// <param name="OccurredOnUtc">The UTC time of the booking.</param>
/// <param name="BookedByUserId">The staff member who booked the slot for the student; <see langword="null"/> when the student did.</param>
public sealed record DrivingLessonBookedDomainEvent(
    Guid SlotId,
    Guid BookingId,
    Guid InstructorId,
    Guid StudentId,
    DateTime StartUtc,
    DateTime OccurredOnUtc,
    Guid? BookedByUserId = null) : IDomainEvent;
