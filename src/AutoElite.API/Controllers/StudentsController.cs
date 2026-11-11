using AutoElite.API.Common;
using AutoElite.Application.StudentCards;
using AutoElite.Application.StudentCards.GetStudentCard;
using Microsoft.AspNetCore.Mvc;

namespace AutoElite.API.Controllers;

/// <summary>
/// The students of the school as one card each: administrators read every card, managers the cards of the contracts
/// of their branch, a student their own.
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
}
