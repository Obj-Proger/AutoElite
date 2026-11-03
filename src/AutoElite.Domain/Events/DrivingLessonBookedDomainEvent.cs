namespace AutoElite.Domain.Events;

/// <summary>Raised when a student books a driving slot; the instructor is notified and a reminder is scheduled.</summary>
/// <param name="SlotId">The identifier of the slot.</param>
/// <param name="BookingId">The identifier of the booking.</param>
/// <param name="InstructorId">The identifier of the driving instructor.</param>
/// <param name="StudentId">The identifier of the student.</param>
/// <param name="StartUtc">The UTC start of the lesson.</param>
/// <param name="OccurredOnUtc">The UTC time of the booking.</param>
public sealed record DrivingLessonBookedDomainEvent(
    Guid SlotId, Guid BookingId, Guid InstructorId, Guid StudentId, DateTime StartUtc, DateTime OccurredOnUtc) : IDomainEvent;
