namespace AutoElite.Application.Notifications.DrivingLessons;

/// <summary>A booked driving lesson as its notifications describe it.</summary>
/// <param name="Slot">The slot of the lesson.</param>
/// <param name="Booking">The booking the notification is about.</param>
/// <param name="BranchId">The branch of the student's contract, which decides who manages the lesson.</param>
/// <param name="LocalStart">The start of the lesson in the local time of that branch.</param>
internal sealed record DrivingLessonDetails(DrivingSlot Slot, DrivingBooking Booking, Guid? BranchId, DateTime LocalStart)
{
    /// <summary>Gets the reference that lets the client open the lesson.</summary>
    public EntityReference Subject => EntityReference.Create(EntityKind.DrivingSlot, Slot.Id).Value;
}
