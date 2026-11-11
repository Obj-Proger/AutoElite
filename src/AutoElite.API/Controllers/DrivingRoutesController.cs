using AutoElite.API.Common;
using AutoElite.Application.DrivingRoutes;
using AutoElite.Application.DrivingRoutes.CreateDrivingRoute;
using AutoElite.Application.DrivingRoutes.DeactivateDrivingRoute;
using AutoElite.Application.DrivingRoutes.GetDrivingRoute;
using AutoElite.Application.DrivingRoutes.GetDrivingRoutes;
using AutoElite.Application.DrivingRoutes.ReactivateDrivingRoute;
using AutoElite.Application.DrivingRoutes.UpdateDrivingRoute;
using Microsoft.AspNetCore.Mvc;

namespace AutoElite.API.Controllers;

/// <summary>
/// The driving routes that instructors describe for their lessons: created by driving instructors, changed by their
/// authors and by administrators.
/// </summary>
/// <param name="dispatcher">The dispatcher of commands and queries.</param>
[Route(ApiRoutes.Prefix + "/driving-routes")]
public sealed class DrivingRoutesController(IDispatcher dispatcher) : ApiControllerBase
{
    /// <summary>Lists the driving routes.</summary>
    /// <param name="authorId">The instructor who described the routes.</param>
    /// <param name="trainingGroundId">The training ground the routes start from.</param>
    /// <param name="isActive">Whether the routes are in use; all when not given.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the request.</param>
    /// <returns>The driving routes.</returns>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<DrivingRouteItem>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDrivingRoutes(
        Guid? authorId, Guid? trainingGroundId, bool? isActive, CancellationToken cancellationToken) =>
        Respond(await dispatcher.Send(new GetDrivingRoutesQuery(authorId, trainingGroundId, isActive), cancellationToken));

    /// <summary>Reads a driving route.</summary>
    /// <param name="routeId">The driving route.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the request.</param>
    /// <returns>The driving route.</returns>
    [HttpGet("{routeId:guid}")]
    [ProducesResponseType<DrivingRouteItem>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<IActionResult> GetDrivingRoute(Guid routeId, CancellationToken cancellationToken) =>
        Respond(await dispatcher.Send(new GetDrivingRouteQuery(routeId), cancellationToken));

    /// <summary>Describes a driving route; the caller is its author.</summary>
    /// <param name="command">The name, the description and the training ground of the route.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the request.</param>
    /// <returns>The identifier of the driving route.</returns>
    [HttpPost]
    [ProducesResponseType<CreatedResource>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<IActionResult> CreateDrivingRoute(CreateDrivingRouteCommand command, CancellationToken cancellationToken) =>
        RespondCreated(await dispatcher.Send(command, cancellationToken), nameof(GetDrivingRoute), "routeId");

    /// <summary>Changes a driving route.</summary>
    /// <param name="routeId">The driving route; it takes the place of the one in the body.</param>
    /// <param name="command">The name, the description and the training ground of the route.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the request.</param>
    [HttpPut("{routeId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<IActionResult> UpdateDrivingRoute(
        Guid routeId, UpdateDrivingRouteCommand command, CancellationToken cancellationToken) =>
        Respond(await dispatcher.Send(command with { RouteId = routeId }, cancellationToken));

    /// <summary>Takes a driving route out of use.</summary>
    /// <param name="routeId">The driving route.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the request.</param>
    [HttpPost("{routeId:guid}/deactivate")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<IActionResult> Deactivate(Guid routeId, CancellationToken cancellationToken) =>
        Respond(await dispatcher.Send(new DeactivateDrivingRouteCommand(routeId), cancellationToken));

    /// <summary>Puts a driving route back in use.</summary>
    /// <param name="routeId">The driving route.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the request.</param>
    [HttpPost("{routeId:guid}/reactivate")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<IActionResult> Reactivate(Guid routeId, CancellationToken cancellationToken) =>
        Respond(await dispatcher.Send(new ReactivateDrivingRouteCommand(routeId), cancellationToken));
}
