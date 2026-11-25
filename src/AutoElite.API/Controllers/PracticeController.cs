using AutoElite.API.Common;
using AutoElite.Application.Practice;
using AutoElite.Application.Practice.AnswerPracticeQuestion;
using AutoElite.Application.Practice.GetPracticeOverview;
using AutoElite.Application.Practice.GetPracticeQuestions;
using Microsoft.AspNetCore.Mvc;

namespace AutoElite.API.Controllers;

/// <summary>
/// The practice of the signed-in student on the questions of the licence category of a contract, outside tests: by
/// topic, as a marathon of every question, or on their mistakes. Each answer is checked at once, with the correct
/// option and the explanation; there is no timer and no outcome.
/// </summary>
/// <param name="dispatcher">The dispatcher of commands and queries.</param>
[Route(ApiRoutes.Prefix + "/practice")]
public sealed class PracticeController(IDispatcher dispatcher) : ApiControllerBase
{
    /// <summary>Reads how far the student has gone through the questions: answered and mistakes, in all and by topic.</summary>
    /// <param name="contractId">A contract of the student.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the request.</param>
    /// <returns>The overview of the practice.</returns>
    [HttpGet]
    [ProducesResponseType<PracticeOverview>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<IActionResult> GetOverview(Guid contractId, CancellationToken cancellationToken) =>
        Respond(await dispatcher.Send(new GetPracticeOverviewQuery(contractId), cancellationToken));

    /// <summary>
    /// Reads a part of the questions of a practice, shuffled by a number the client chooses once for the practice, so
    /// that the parts read with it follow one order; the correct options are left out.
    /// </summary>
    /// <param name="contractId">A contract of the student, whose licence category chooses the questions.</param>
    /// <param name="mode">Which questions are practised.</param>
    /// <param name="topicId">The topic, for the practice of a topic.</param>
    /// <param name="seed">The number that shuffles the questions.</param>
    /// <param name="offset">How many questions of the practice come before the part.</param>
    /// <param name="count">How many questions the part has, from 1 to 50.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the request.</param>
    /// <returns>The part, with the number of questions of the practice.</returns>
    [HttpGet("questions")]
    [ProducesResponseType<PracticeQuestionPage>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<IActionResult> GetQuestions(
        Guid contractId,
        PracticeMode mode,
        Guid? topicId,
        int seed,
        int offset,
        int count,
        CancellationToken cancellationToken) =>
        Respond(await dispatcher.Send(
            new GetPracticeQuestionsQuery(contractId, mode, topicId, seed, offset, count), cancellationToken));

    /// <summary>Checks an answer of the student at once and records it among their answers and mistakes.</summary>
    /// <param name="command">The contract, the question and the chosen option.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the request.</param>
    /// <returns>Whether the answer was right, with the correct option and the explanation.</returns>
    [HttpPost("answers")]
    [ProducesResponseType<PracticeAnswerResult>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<IActionResult> Answer(AnswerPracticeQuestionCommand command, CancellationToken cancellationToken) =>
        Respond(await dispatcher.Send(command, cancellationToken));
}
