using AutoElite.API.Common;
using AutoElite.Application.Exams;
using AutoElite.Application.Exams.CancelExam;
using AutoElite.Application.Exams.CorrectExamResult;
using AutoElite.Application.Exams.GetExam;
using AutoElite.Application.Exams.GetExamSchedule;
using AutoElite.Application.Exams.GetMyExams;
using AutoElite.Application.Exams.RecordPracticeExamResult;
using AutoElite.Application.Exams.RescheduleExam;
using AutoElite.Application.Exams.ScheduleExam;
using Microsoft.AspNetCore.Mvc;

namespace AutoElite.API.Controllers;

/// <summary>
/// The internal exams of the study groups: scheduled, moved and cancelled by the responsible theory instructor of the
/// group, with results recorded by driving instructors for practice and taken as a test for theory, read by the staff
/// and by students in their own list. Times are in UTC.
/// </summary>
/// <param name="dispatcher">The dispatcher of commands and queries.</param>
[Route(ApiRoutes.Prefix + "/exams")]
public sealed class ExamsController(IDispatcher dispatcher) : ApiControllerBase
{
    /// <summary>
    /// Reads the exams scheduled within a period of at most 92 days, the earliest first. A theory instructor sees the
    /// exams of their own groups.
    /// </summary>
    /// <param name="fromUtc">The start of the period, in UTC.</param>
    /// <param name="toUtc">The end of the period, in UTC.</param>
    /// <param name="kind">Only theory or only practice exams.</param>
    /// <param name="groupId">The study group.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the request.</param>
    /// <returns>The exams.</returns>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<ExamItem>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSchedule(
        DateTime fromUtc, DateTime toUtc, ExamKind? kind, Guid? groupId, CancellationToken cancellationToken) =>
        Respond(await dispatcher.Send(new GetExamScheduleQuery(fromUtc, toUtc, kind, groupId), cancellationToken));

    /// <summary>Lists the exams of the signed-in student with their own results, the latest first.</summary>
    /// <param name="cancellationToken">A token that signals cancellation of the request.</param>
    /// <returns>The exams.</returns>
    [HttpGet("/" + ApiRoutes.Prefix + "/me/exams")]
    [ProducesResponseType<IReadOnlyList<StudentExamItem>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyExams(CancellationToken cancellationToken) =>
        Respond(await dispatcher.Send(new GetMyExamsQuery(), cancellationToken));

    /// <summary>Reads an exam with the results of all of its students.</summary>
    /// <param name="examId">The exam.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the request.</param>
    /// <returns>The exam.</returns>
    [HttpGet("{examId:guid}")]
    [ProducesResponseType<ExamDetails>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<IActionResult> GetExam(Guid examId, CancellationToken cancellationToken) =>
        Respond(await dispatcher.Send(new GetExamQuery(examId), cancellationToken));

    /// <summary>
    /// Schedules an exam in the future for a group the signed-in instructor is responsible for; the students of the
    /// group are notified. A theory exam is taken as a test from an active template for the category of the group; a
    /// practice exam has no template.
    /// </summary>
    /// <param name="command">The group, the kind, the time and the template.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the request.</param>
    /// <returns>The identifier of the exam.</returns>
    [HttpPost]
    [ProducesResponseType<CreatedResource>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<IActionResult> Schedule(ScheduleExamCommand command, CancellationToken cancellationToken) =>
        RespondCreated(await dispatcher.Send(command, cancellationToken), nameof(GetExam), "examId");

    /// <summary>Moves a scheduled exam that has no results yet; the students of the group are notified.</summary>
    /// <param name="examId">The exam; it takes the place of the one in the body.</param>
    /// <param name="command">The new time, in the future.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the request.</param>
    [HttpPut("{examId:guid}/time")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<IActionResult> Reschedule(Guid examId, RescheduleExamCommand command, CancellationToken cancellationToken) =>
        Respond(await dispatcher.Send(command with { ExamId = examId }, cancellationToken));

    /// <summary>Cancels a scheduled exam that has no results yet, with the reason; the students are notified.</summary>
    /// <param name="examId">The exam; it takes the place of the one in the body.</param>
    /// <param name="command">The reason.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the request.</param>
    [HttpPost("{examId:guid}/cancel")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<IActionResult> Cancel(Guid examId, CancelExamCommand command, CancellationToken cancellationToken) =>
        Respond(await dispatcher.Send(command with { ExamId = examId }, cancellationToken));

    /// <summary>
    /// Records the result of a current member of the group at a practice exam that has begun; the driving instructors
    /// who see the branch of the group record the results.
    /// </summary>
    /// <param name="examId">The exam; it takes the place of the one in the body.</param>
    /// <param name="command">The contract of the student, whether they passed, the score and a comment.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the request.</param>
    [HttpPost("{examId:guid}/results")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<IActionResult> RecordPracticeResult(
        Guid examId, RecordPracticeExamResultCommand command, CancellationToken cancellationToken) =>
        Respond(await dispatcher.Send(command with { ExamId = examId }, cancellationToken));

    /// <summary>
    /// Corrects a recorded result, for example after an appeal: a practice result by the driving instructors who see the
    /// branch of the group, a theory result by the responsible instructor of the group.
    /// </summary>
    /// <param name="examId">The exam; it takes the place of the one in the body.</param>
    /// <param name="contractId">The contract whose result is corrected; it takes the place of the one in the body.</param>
    /// <param name="command">Whether the student passed, the score and the reason of the correction.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the request.</param>
    [HttpPut("{examId:guid}/results/{contractId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<IActionResult> CorrectResult(
        Guid examId, Guid contractId, CorrectExamResultCommand command, CancellationToken cancellationToken) =>
        Respond(await dispatcher.Send(command with { ExamId = examId, ContractId = contractId }, cancellationToken));
}
