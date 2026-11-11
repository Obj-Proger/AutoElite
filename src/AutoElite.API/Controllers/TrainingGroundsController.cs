using AutoElite.API.Common;
using AutoElite.Application.TrainingGrounds;
using AutoElite.Application.TrainingGrounds.CreateTrainingGround;
using AutoElite.Application.TrainingGrounds.DeactivateTrainingGround;
using AutoElite.Application.TrainingGrounds.GetTrainingGround;
using AutoElite.Application.TrainingGrounds.GetTrainingGrounds;
using AutoElite.Application.TrainingGrounds.ReactivateTrainingGround;
using AutoElite.Application.TrainingGrounds.UpdateTrainingGround;
using Microsoft.AspNetCore.Mvc;

namespace AutoElite.API.Controllers;

/// <summary>The training grounds where driving lessons start: read by driving instructors, managed by administrators.</summary>
/// <param name="dispatcher">The dispatcher of commands and queries.</param>
[Route(ApiRoutes.Prefix + "/training-grounds")]
public sealed class TrainingGroundsController(IDispatcher dispatcher) : ApiControllerBase
{
    /// <summary>Lists the training grounds.</summary>
    /// <param name="branchId">The branch of the grounds.</param>
    /// <param name="isActive">Whether the grounds are active; all when not given.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the request.</param>
    /// <returns>The training grounds.</returns>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<TrainingGroundItem>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTrainingGrounds(Guid? branchId, bool? isActive, CancellationToken cancellationToken) =>
        Respond(await dispatcher.Send(new GetTrainingGroundsQuery(branchId, isActive), cancellationToken));

    /// <summary>Reads a training ground.</summary>
    /// <param name="groundId">The training ground.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the request.</param>
    /// <returns>The training ground.</returns>
    [HttpGet("{groundId:guid}")]
    [ProducesResponseType<TrainingGroundItem>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<IActionResult> GetTrainingGround(Guid groundId, CancellationToken cancellationToken) =>
        Respond(await dispatcher.Send(new GetTrainingGroundQuery(groundId), cancellationToken));

    /// <summary>Adds a training ground.</summary>
    /// <param name="command">The name, the address, a description and the branch of the ground.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the request.</param>
    /// <returns>The identifier of the training ground.</returns>
    [HttpPost]
    [ProducesResponseType<CreatedResource>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<IActionResult> CreateTrainingGround(CreateTrainingGroundCommand command, CancellationToken cancellationToken) =>
        RespondCreated(await dispatcher.Send(command, cancellationToken), nameof(GetTrainingGround), "groundId");

    /// <summary>Changes a training ground.</summary>
    /// <param name="groundId">The training ground; it takes the place of the one in the body.</param>
    /// <param name="command">The name, the address, a description and the branch of the ground.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the request.</param>
    [HttpPut("{groundId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<IActionResult> UpdateTrainingGround(
        Guid groundId, UpdateTrainingGroundCommand command, CancellationToken cancellationToken) =>
        Respond(await dispatcher.Send(command with { GroundId = groundId }, cancellationToken));

    /// <summary>Takes a training ground out of use.</summary>
    /// <param name="groundId">The training ground.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the request.</param>
    [HttpPost("{groundId:guid}/deactivate")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<IActionResult> Deactivate(Guid groundId, CancellationToken cancellationToken) =>
        Respond(await dispatcher.Send(new DeactivateTrainingGroundCommand(groundId), cancellationToken));

    /// <summary>Puts a training ground back in use.</summary>
    /// <param name="groundId">The training ground.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the request.</param>
    [HttpPost("{groundId:guid}/reactivate")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<IActionResult> Reactivate(Guid groundId, CancellationToken cancellationToken) =>
        Respond(await dispatcher.Send(new ReactivateTrainingGroundCommand(groundId), cancellationToken));
}
