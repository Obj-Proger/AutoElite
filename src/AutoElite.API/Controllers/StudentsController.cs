using AutoElite.API.Common;
using AutoElite.Application.StudentCards;
using AutoElite.Application.StudentCards.GetStudentCard;
using AutoElite.Application.StudentCards.GetStudentProgress;
using Microsoft.AspNetCore.Mvc;

namespace AutoElite.API.Controllers;

/// <summary>
/// The students of the school as one card each: administrators read every card, managers the cards of the contracts
/// of their branch, a student their own; their instructors read their progress, without the money.
/// </summary>
/// <param name="dispatcher">The dispatcher of commands and queries.</param>
[Route(ApiRoutes.Prefix + "/students")]
public sealed class StudentsController(IDispatcher dispatcher) : ApiControllerBase
{
    /// <summary>
    /// Reads the card of a student: their contracts with the group, the attendance, the driving lessons, the tests, the
    /// exams and the debt of each.
    /// </summary>
    /// <param name="studentId">The student.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the request.</param>
    /// <returns>The card.</returns>
    [HttpGet("{studentId:guid}/card")]
    [ProducesResponseType<StudentCard>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<IActionResult> GetCard(Guid studentId, CancellationToken cancellationToken) =>
        Respond(await dispatcher.Send(new GetStudentCardQuery(studentId), cancellationToken));

    /// <summary>
    /// Reads the progress of a student the signed-in instructor teaches: their contracts with the group and the
    /// attendance, the driving lessons, the tests and the exams, without the money of the contracts.
    /// </summary>
    /// <param name="studentId">The student.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the request.</param>
    /// <returns>The progress.</returns>
    [HttpGet("{studentId:guid}/progress")]
    [ProducesResponseType<StudentProgress>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<IActionResult> GetProgress(Guid studentId, CancellationToken cancellationToken) =>
        Respond(await dispatcher.Send(new GetStudentProgressQuery(studentId), cancellationToken));
}
