using AutoElite.API.Common;
using AutoElite.Application.Exams.StartExamTest;
using AutoElite.Application.TestSessions;
using AutoElite.Application.TestSessions.AnswerTestQuestion;
using AutoElite.Application.TestSessions.GetContractTests;
using AutoElite.Application.TestSessions.GetTestSession;
using AutoElite.Application.TestSessions.StartTest;
using Microsoft.AspNetCore.Mvc;

namespace AutoElite.API.Controllers;

/// <summary>The state of a test attempt after an answer.</summary>
/// <param name="Status">In progress while questions are left and the time has not run out; passed or failed after.</param>
public sealed record TestAnswerOutcome(TestSessionStatus Status);

/// <summary>
/// The test attempts of students: practice tests from a template and theory exams, taken by the student and read by
/// the student and by managers and administrators. Whether an answer is correct is shown only after the attempt.
/// </summary>
/// <param name="dispatcher">The dispatcher of commands and queries.</param>
[Route(ApiRoutes.Prefix + "/test-sessions")]
public sealed class TestSessionsController(IDispatcher dispatcher) : ApiControllerBase
{
    /// <summary>
    /// Reads an attempt with its questions: while it is in progress, the chosen answers only; after it, the correct
    /// answers and the explanations too.
    /// </summary>
    /// <param name="sessionId">The attempt.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the request.</param>
    /// <returns>The attempt.</returns>
    [HttpGet("{sessionId:guid}")]
    [ProducesResponseType<TestSessionDetails>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<IActionResult> GetSession(Guid sessionId, CancellationToken cancellationToken) =>
        Respond(await dispatcher.Send(new GetTestSessionQuery(sessionId), cancellationToken));

    /// <summary>Lists all test attempts of a contract, the latest first.</summary>
    /// <param name="contractId">The contract.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the request.</param>
    /// <returns>The attempts.</returns>
    [HttpGet("/" + ApiRoutes.Prefix + "/contracts/{contractId:guid}/test-sessions")]
    [ProducesResponseType<IReadOnlyList<TestSessionItem>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<IActionResult> GetContractSessions(Guid contractId, CancellationToken cancellationToken) =>
        Respond(await dispatcher.Send(new GetContractTestsQuery(contractId), cancellationToken));

    /// <summary>
    /// Starts a practice test of the signed-in student on a contract from an active template for its category: the
    /// questions are drawn at random for each block and the timer starts. A contract has one attempt in progress at a
    /// time.
    /// </summary>
    /// <param name="command">The contract and the template.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the request.</param>
    /// <returns>The identifier of the attempt.</returns>
    [HttpPost]
    [ProducesResponseType<CreatedResource>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<IActionResult> StartTest(StartTestCommand command, CancellationToken cancellationToken) =>
        RespondCreated(await dispatcher.Send(command, cancellationToken), nameof(GetSession), "sessionId");

    /// <summary>
    /// Starts the attempt of the signed-in student at a theory exam of their group once the exam has begun; its result
    /// becomes the exam result when it finishes. Each contract takes the exam once.
    /// </summary>
    /// <param name="examId">The exam; it takes the place of the one in the body.</param>
    /// <param name="command">The contract of the student.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the request.</param>
    /// <returns>The identifier of the attempt.</returns>
    [HttpPost("/" + ApiRoutes.Prefix + "/exams/{examId:guid}/test-session")]
    [ProducesResponseType<CreatedResource>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<IActionResult> StartExamTest(Guid examId, StartExamTestCommand command, CancellationToken cancellationToken) =>
        RespondCreated(await dispatcher.Send(command with { ExamId = examId }, cancellationToken), nameof(GetSession), "sessionId");

    /// <summary>
    /// Records the answer of the signed-in student to a question of their attempt. A mistake the rules allow adds
    /// questions and time; an attempt whose time has run out is closed instead.
    /// </summary>
    /// <param name="sessionId">The attempt; it takes the place of the one in the body.</param>
    /// <param name="command">The question of the attempt and the chosen option.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the request.</param>
    /// <returns>The state of the attempt after the answer.</returns>
    [HttpPost("{sessionId:guid}/answers")]
    [ProducesResponseType<TestAnswerOutcome>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<IActionResult> Answer(Guid sessionId, AnswerTestQuestionCommand command, CancellationToken cancellationToken)
    {
        var result = await dispatcher.Send(command with { SessionId = sessionId }, cancellationToken);

        return result.IsSuccess ? Ok(new TestAnswerOutcome(result.Value)) : Failure(result.Error);
    }
}
