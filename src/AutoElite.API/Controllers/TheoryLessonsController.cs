using AutoElite.API.Common;
using AutoElite.Application.TheoryLessons;
using AutoElite.Application.TheoryLessons.CancelTheoryLesson;
using AutoElite.Application.TheoryLessons.ChangeTheoryLessonInstructor;
using AutoElite.Application.TheoryLessons.GetMyTheorySchedule;
using AutoElite.Application.TheoryLessons.GetTheoryLesson;
using AutoElite.Application.TheoryLessons.GetTheorySchedule;
using AutoElite.Application.TheoryLessons.MarkAttendance;
using AutoElite.Application.TheoryLessons.RescheduleTheoryLesson;
using AutoElite.Application.TheoryLessons.ScheduleTheoryLesson;
using Microsoft.AspNetCore.Mvc;

namespace AutoElite.API.Controllers;

/// <summary>
/// The theory lessons of the study groups: scheduled, changed and marked by the theory instructor of the group or of
/// the lesson, read by managers and administrators, and by students in their own schedule. Times are in UTC.
/// </summary>
/// <param name="dispatcher">The dispatcher of commands and queries.</param>
[Route(ApiRoutes.Prefix + "/theory-lessons")]
public sealed class TheoryLessonsController(IDispatcher dispatcher) : ApiControllerBase
{
    /// <summary>
    /// Reads the schedule: the lessons that overlap a period of at most 92 days, the earliest first. A theory instructor
    /// sees the lessons they give and those of the groups they are responsible for.
    /// </summary>
    /// <param name="fromUtc">The start of the period, in UTC.</param>
    /// <param name="toUtc">The end of the period, in UTC.</param>
    /// <param name="instructorId">The instructor who gives the lessons or is responsible for their groups.</param>
    /// <param name="groupId">The study group.</param>
    /// <param name="branchId">The branch of the groups.</param>
    /// <param name="status">The status of the lessons.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the request.</param>
    /// <returns>The lessons.</returns>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<TheoryLessonItem>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSchedule(
        DateTime fromUtc,
        DateTime toUtc,
        Guid? instructorId,
        Guid? groupId,
        Guid? branchId,
        TheoryLessonStatus? status,
        CancellationToken cancellationToken) =>
        Respond(await dispatcher.Send(
            new GetTheoryScheduleQuery(fromUtc, toUtc, instructorId, groupId, branchId, status), cancellationToken));

    /// <summary>
    /// Reads the schedule of the signed-in student: the lessons of all their groups that overlap a period of at most
    /// 92 days, with their own attendance.
    /// </summary>
    /// <param name="fromUtc">The start of the period, in UTC.</param>
    /// <param name="toUtc">The end of the period, in UTC.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the request.</param>
    /// <returns>The lessons.</returns>
    [HttpGet("/" + ApiRoutes.Prefix + "/me/theory-lessons")]
    [ProducesResponseType<IReadOnlyList<StudentTheoryLessonItem>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMySchedule(DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken) =>
        Respond(await dispatcher.Send(new GetMyTheoryScheduleQuery(fromUtc, toUtc), cancellationToken));

    /// <summary>Reads a lesson with the students expected at it and their attendance.</summary>
    /// <param name="lessonId">The lesson.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the request.</param>
    /// <returns>The lesson.</returns>
    [HttpGet("{lessonId:guid}")]
    [ProducesResponseType<TheoryLessonDetails>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<IActionResult> GetLesson(Guid lessonId, CancellationToken cancellationToken) =>
        Respond(await dispatcher.Send(new GetTheoryLessonQuery(lessonId), cancellationToken));

    /// <summary>
    /// Schedules a lesson on a topic of the course for a group the signed-in instructor is responsible for. The lesson
    /// starts in the future and lasts at most 8 hours; the instructor who gives it is the signed-in one unless another
    /// is given.
    /// </summary>
    /// <param name="command">The group, the topic, the time and the instructor.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the request.</param>
    /// <returns>The identifier of the lesson.</returns>
    [HttpPost]
    [ProducesResponseType<CreatedResource>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<IActionResult> Schedule(ScheduleTheoryLessonCommand command, CancellationToken cancellationToken) =>
        RespondCreated(await dispatcher.Send(command, cancellationToken), nameof(GetLesson), "lessonId");

    /// <summary>Moves a scheduled lesson that has no attendance yet; the students of the group are notified.</summary>
    /// <param name="lessonId">The lesson; it takes the place of the one in the body.</param>
    /// <param name="command">The new start and end.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the request.</param>
    [HttpPut("{lessonId:guid}/time")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<IActionResult> Reschedule(
        Guid lessonId, RescheduleTheoryLessonCommand command, CancellationToken cancellationToken) =>
        Respond(await dispatcher.Send(command with { LessonId = lessonId }, cancellationToken));

    /// <summary>Hands a scheduled lesson to a substitute theory instructor who is free at that time.</summary>
    /// <param name="lessonId">The lesson; it takes the place of the one in the body.</param>
    /// <param name="command">The instructor who will give the lesson.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the request.</param>
    [HttpPut("{lessonId:guid}/instructor")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<IActionResult> ChangeInstructor(
        Guid lessonId, ChangeTheoryLessonInstructorCommand command, CancellationToken cancellationToken) =>
        Respond(await dispatcher.Send(command with { LessonId = lessonId }, cancellationToken));

    /// <summary>Cancels a scheduled lesson that has no attendance yet, with the reason; the students are notified.</summary>
    /// <param name="lessonId">The lesson; it takes the place of the one in the body.</param>
    /// <param name="command">The reason.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the request.</param>
    [HttpPost("{lessonId:guid}/cancel")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<IActionResult> Cancel(Guid lessonId, CancelTheoryLessonCommand command, CancellationToken cancellationToken) =>
        Respond(await dispatcher.Send(command with { LessonId = lessonId }, cancellationToken));

    /// <summary>
    /// Marks or corrects the attendance of students at a lesson that has started, usually the whole group at once;
    /// students not in the marks keep the marks they have. The lesson is completed once every current member of the
    /// group is marked. All marks are saved together or none is.
    /// </summary>
    /// <param name="lessonId">The lesson; it takes the place of the one in the body.</param>
    /// <param name="command">The marks, at most one per contract.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the request.</param>
    [HttpPut("{lessonId:guid}/attendance")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<IActionResult> MarkAttendance(
        Guid lessonId, MarkAttendanceCommand command, CancellationToken cancellationToken) =>
        Respond(await dispatcher.Send(command with { LessonId = lessonId }, cancellationToken));
}
