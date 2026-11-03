namespace AutoElite.Domain.Events;

/// <summary>Raised when a student gives a driving lesson a low rating; the responsible managers are notified.</summary>
/// <param name="SlotId">The identifier of the slot.</param>
/// <param name="BookingId">The identifier of the booking.</param>
/// <param name="InstructorId">The identifier of the driving instructor.</param>
/// <param name="StudentId">The identifier of the student.</param>
/// <param name="Rating">The rating the student gave.</param>
/// <param name="OccurredOnUtc">The UTC time of the rating.</param>
public sealed record LowDrivingLessonRatingDomainEvent(
    Guid SlotId, Guid BookingId, Guid InstructorId, Guid StudentId, int Rating, DateTime OccurredOnUtc) : IDomainEvent;
