namespace AutoElite.Domain.Events;

/// <summary>Raised when a driving lesson is completed; the student is invited to rate it.</summary>
/// <param name="SlotId">The identifier of the slot.</param>
/// <param name="BookingId">The identifier of the booking.</param>
/// <param name="InstructorId">The identifier of the driving instructor.</param>
/// <param name="StudentId">The identifier of the student.</param>
/// <param name="OccurredOnUtc">The UTC time of completion.</param>
public sealed record DrivingLessonCompletedDomainEvent(
    Guid SlotId, Guid BookingId, Guid InstructorId, Guid StudentId, DateTime OccurredOnUtc) : IDomainEvent;
