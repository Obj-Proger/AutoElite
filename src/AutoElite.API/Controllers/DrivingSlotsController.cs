using AutoElite.API.Common;
using AutoElite.Application.DrivingSlots;
using AutoElite.Application.DrivingSlots.BookDrivingSlot;
using AutoElite.Application.DrivingSlots.CancelDrivingBooking;
using AutoElite.Application.DrivingSlots.CompleteDrivingLesson;
using AutoElite.Application.DrivingSlots.GetAvailableDrivingSlots;
using AutoElite.Application.DrivingSlots.GetDrivingSchedule;
using AutoElite.Application.DrivingSlots.GetDrivingSlot;
using AutoElite.Application.DrivingSlots.GetMyDrivingLessons;
using AutoElite.Application.DrivingSlots.MarkDrivingNoShow;
using AutoElite.Application.DrivingSlots.OfferDrivingSlot;
using AutoElite.Application.DrivingSlots.RateDrivingLesson;
using AutoElite.Application.DrivingSlots.WithdrawDrivingSlot;
using Microsoft.AspNetCore.Mvc;

namespace AutoElite.API.Controllers;

/// <summary>
/// The practice slots of driving instructors and the lessons booked in them: offered, completed and withdrawn by
/// their instructor, booked and rated by students, read by managers and administrators. Times are in UTC.
/// </summary>
/// <param name="dispatcher">The dispatcher of commands and queries.</param>
[Route(ApiRoutes.Prefix + "/driving-slots")]
public sealed class DrivingSlotsController(IDispatcher dispatcher) : ApiControllerBase
{
    /// <summary>
    /// Reads the schedule: the slots that overlap a period of at most 92 days with the bookings that hold them, the
    /// earliest first. A driving instructor sees only their own slots.
    /// </summary>
    /// <param name="fromUtc">The start of the period, in UTC.</param>
    /// <param name="toUtc">The end of the period, in UTC.</param>
    /// <param name="instructorId">The instructor of the slots.</param>
    /// <param name="status">The status of the slots.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the request.</param>
    /// <returns>The slots.</returns>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<DrivingScheduleItem>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSchedule(
        DateTime fromUtc, DateTime toUtc, Guid? instructorId, DrivingSlotStatus? status, CancellationToken cancellationToken) =>
        Respond(await dispatcher.Send(new GetDrivingScheduleQuery(fromUtc, toUtc, instructorId, status), cancellationToken));

    /// <summary>
    /// Finds the slots a contract of the signed-in student can book in a period of at most 92 days, the earliest
    /// first: slots that have not started, for the licence category and the gearbox of the contract.
    /// </summary>
    /// <param name="contractId">A contract of the student.</param>
    /// <param name="fromUtc">The start of the period, in UTC.</param>
    /// <param name="toUtc">The end of the period, in UTC.</param>
    /// <param name="instructorId">The instructor of the slots.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the request.</param>
    /// <returns>The slots.</returns>
    [HttpGet("available")]
    [ProducesResponseType<IReadOnlyList<DrivingSlotItem>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<IActionResult> GetAvailable(
        Guid contractId, DateTime fromUtc, DateTime toUtc, Guid? instructorId, CancellationToken cancellationToken) =>
        Respond(await dispatcher.Send(
            new GetAvailableDrivingSlotsQuery(contractId, fromUtc, toUtc, instructorId), cancellationToken));

    /// <summary>
    /// Reads the driving lessons of the signed-in student in a period of at most 92 days, the earliest first: every
    /// booking, cancelled ones included, with the hours, the grades and the note of completed lessons.
    /// </summary>
    /// <param name="fromUtc">The start of the period, in UTC.</param>
    /// <param name="toUtc">The end of the period, in UTC.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the request.</param>
    /// <returns>The lessons.</returns>
    [HttpGet("/" + ApiRoutes.Prefix + "/me/driving-lessons")]
    [ProducesResponseType<IReadOnlyList<StudentDrivingLessonItem>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyLessons(DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken) =>
        Respond(await dispatcher.Send(new GetMyDrivingLessonsQuery(fromUtc, toUtc), cancellationToken));

    /// <summary>Reads a slot with all of its bookings, cancelled ones included.</summary>
    /// <param name="slotId">The slot.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the request.</param>
    /// <returns>The slot.</returns>
    [HttpGet("{slotId:guid}")]
    [ProducesResponseType<DrivingSlotDetails>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<IActionResult> GetSlot(Guid slotId, CancellationToken cancellationToken) =>
        Respond(await dispatcher.Send(new GetDrivingSlotQuery(slotId), cancellationToken));

    /// <summary>
    /// Offers a slot of the signed-in instructor that starts in the future and lasts at most 4 hours. The slot takes the
    /// licence category and the gearbox of the vehicle.
    /// </summary>
    /// <param name="command">The vehicle, the time, the training ground and the route.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the request.</param>
    /// <returns>The identifier of the slot.</returns>
    [HttpPost]
    [ProducesResponseType<CreatedResource>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<IActionResult> Offer(OfferDrivingSlotCommand command, CancellationToken cancellationToken) =>
        RespondCreated(await dispatcher.Send(command, cancellationToken), nameof(GetSlot), "slotId");

    /// <summary>
    /// Withdraws an available or booked slot of the signed-in instructor, with the reason; a booking of the slot is
    /// cancelled and its student is notified.
    /// </summary>
    /// <param name="slotId">The slot; it takes the place of the one in the body.</param>
    /// <param name="command">The reason.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the request.</param>
    [HttpPost("{slotId:guid}/withdraw")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<IActionResult> Withdraw(Guid slotId, WithdrawDrivingSlotCommand command, CancellationToken cancellationToken) =>
        Respond(await dispatcher.Send(command with { SlotId = slotId }, cancellationToken));

    /// <summary>
    /// Books an available slot for an active contract of the signed-in student whose licence category and gearbox match
    /// the vehicle.
    /// </summary>
    /// <param name="slotId">The slot; it takes the place of the one in the body.</param>
    /// <param name="command">The contract.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the request.</param>
    [HttpPost("{slotId:guid}/book")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<IActionResult> Book(Guid slotId, BookDrivingSlotCommand command, CancellationToken cancellationToken) =>
        Respond(await dispatcher.Send(command with { SlotId = slotId }, cancellationToken));

    /// <summary>
    /// Cancels the booking of a slot before the lesson starts, with the reason; the slot becomes available again. A
    /// student cancels their own booking until the notice the school sets; the instructor of the slot and the staff of
    /// the contract, until the start.
    /// </summary>
    /// <param name="slotId">The slot; it takes the place of the one in the body.</param>
    /// <param name="command">The reason.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the request.</param>
    [HttpPost("{slotId:guid}/cancel-booking")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<IActionResult> CancelBooking(
        Guid slotId, CancelDrivingBookingCommand command, CancellationToken cancellationToken) =>
        Respond(await dispatcher.Send(command with { SlotId = slotId }, cancellationToken));

    /// <summary>
    /// Completes a booked lesson of the signed-in instructor that has started, with the actual hours, the skill grades
    /// and a note; the hours count towards the practice of the contract.
    /// </summary>
    /// <param name="slotId">The slot; it takes the place of the one in the body.</param>
    /// <param name="command">The hours, the grades and the note.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the request.</param>
    [HttpPost("{slotId:guid}/complete")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<IActionResult> Complete(Guid slotId, CompleteDrivingLessonCommand command, CancellationToken cancellationToken) =>
        Respond(await dispatcher.Send(command with { SlotId = slotId }, cancellationToken));

    /// <summary>
    /// Records that the student did not come to a booked lesson of the signed-in instructor that has started; no hours
    /// are counted.
    /// </summary>
    /// <param name="slotId">The slot; it takes the place of the one in the body.</param>
    /// <param name="command">The note, if any; an empty object for none.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the request.</param>
    [HttpPost("{slotId:guid}/no-show")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<IActionResult> MarkNoShow(Guid slotId, MarkDrivingNoShowCommand command, CancellationToken cancellationToken) =>
        Respond(await dispatcher.Send(command with { SlotId = slotId }, cancellationToken));

    /// <summary>
    /// Rates a completed lesson of the signed-in student, once; a low rating notifies the responsible managers.
    /// </summary>
    /// <param name="slotId">The slot; it takes the place of the one in the body.</param>
    /// <param name="command">The rating from 1 to 5 and a comment.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the request.</param>
    [HttpPost("{slotId:guid}/rating")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<IActionResult> Rate(Guid slotId, RateDrivingLessonCommand command, CancellationToken cancellationToken) =>
        Respond(await dispatcher.Send(command with { SlotId = slotId }, cancellationToken));
}
