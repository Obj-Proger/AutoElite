namespace AutoElite.Domain.Events;

/// <summary>
/// Raised when a booking is cancelled, either on its own or because the slot was withdrawn; the other party is
/// notified and the reminder is dropped.
/// </summary>
/// <param name="SlotId">The identifier of the slot.</param>
/// <param name="BookingId">The identifier of the booking.</param>
/// <param name="InstructorId">The identifier of the driving instructor.</param>
/// <param name="StudentId">The identifier of the student.</param>
/// <param name="CancelledByUserId">The user who cancelled, or <see langword="null"/> for the system.</param>
/// <param name="Reason">Why the booking was cancelled.</param>
/// <param name="StartUtc">The UTC start of the cancelled lesson.</param>
/// <param name="OccurredOnUtc">The UTC time of the cancellation.</param>
public sealed record DrivingBookingCancelledDomainEvent(
    Guid SlotId,
    Guid BookingId,
    Guid InstructorId,
    Guid StudentId,
    Guid? CancelledByUserId,
    string Reason,
    DateTime StartUtc,
    DateTime OccurredOnUtc) : IDomainEvent;
