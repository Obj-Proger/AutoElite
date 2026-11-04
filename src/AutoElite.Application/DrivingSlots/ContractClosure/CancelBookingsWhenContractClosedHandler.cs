using AutoElite.Application.Common.Events;

namespace AutoElite.Application.DrivingSlots.ContractClosure;

/// <summary>
/// Cancels the bookings of a completed or terminated contract for lessons that had not started when it was closed,
/// so that the slots become available to other students.
/// </summary>
/// <remarks>
/// The cancellation has no user, which marks it as made by the system; the notification of the student is worded
/// from the event, and <see cref="Reason"/> stays in the booking for the staff.
/// </remarks>
internal sealed class CancelBookingsWhenContractClosedHandler(IDrivingSlotRepository slotRepository)
    : IDomainEventHandler<ContractClosedDomainEvent>
{
    /// <summary>The reason recorded on the cancelled bookings.</summary>
    public const string Reason = "The contract was closed.";

    /// <inheritdoc />
    public async Task Handle(ContractClosedDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        var slots = await slotRepository.GetBookedForContractAfterAsync(
            domainEvent.ContractId, domainEvent.OccurredOnUtc, cancellationToken);

        // Cancelling at the time of closing keeps the history right even if the event is delivered late.
        foreach (var slot in slots)
            slot.CancelBooking(null, Reason, TimeSpan.Zero, domainEvent.OccurredOnUtc);
    }
}
