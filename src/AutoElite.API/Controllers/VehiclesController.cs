using AutoElite.API.Common;
using AutoElite.Application.Vehicles;
using AutoElite.Application.Vehicles.DeactivateVehicle;
using AutoElite.Application.Vehicles.GetVehicle;
using AutoElite.Application.Vehicles.GetVehicles;
using AutoElite.Application.Vehicles.ReactivateVehicle;
using AutoElite.Application.Vehicles.RegisterVehicle;
using AutoElite.Application.Vehicles.UpdateVehicle;
using Microsoft.AspNetCore.Mvc;

namespace AutoElite.API.Controllers;

/// <summary>The vehicles of the school: read by driving instructors, managed by administrators.</summary>
/// <param name="dispatcher">The dispatcher of commands and queries.</param>
[Route(ApiRoutes.Prefix + "/vehicles")]
public sealed class VehiclesController(IDispatcher dispatcher) : ApiControllerBase
{
    /// <summary>Lists the vehicles.</summary>
    /// <param name="category">The licence category of the vehicles.</param>
    /// <param name="transmission">The transmission of the vehicles.</param>
    /// <param name="branchId">The branch of the vehicles.</param>
    /// <param name="isActive">Whether the vehicles are in use; all when not given.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the request.</param>
    /// <returns>The vehicles.</returns>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<VehicleItem>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetVehicles(
        LicenseCategory? category,
        Transmission? transmission,
        Guid? branchId,
        bool? isActive,
        CancellationToken cancellationToken) =>
        Respond(await dispatcher.Send(new GetVehiclesQuery(category, transmission, branchId, isActive), cancellationToken));

    /// <summary>Reads a vehicle.</summary>
    /// <param name="vehicleId">The vehicle.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the request.</param>
    /// <returns>The vehicle.</returns>
    [HttpGet("{vehicleId:guid}")]
    [ProducesResponseType<VehicleItem>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<IActionResult> GetVehicle(Guid vehicleId, CancellationToken cancellationToken) =>
        Respond(await dispatcher.Send(new GetVehicleQuery(vehicleId), cancellationToken));

    /// <summary>Registers a vehicle.</summary>
    /// <param name="command">The plate, the make, the model, the year, the category, the transmission and the branch.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the request.</param>
    /// <returns>The identifier of the vehicle.</returns>
    [HttpPost]
    [ProducesResponseType<CreatedResource>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<IActionResult> RegisterVehicle(RegisterVehicleCommand command, CancellationToken cancellationToken) =>
        RespondCreated(await dispatcher.Send(command, cancellationToken), nameof(GetVehicle), "vehicleId");

    /// <summary>Changes a vehicle.</summary>
    /// <param name="vehicleId">The vehicle; it takes the place of the one in the body.</param>
    /// <param name="command">The plate, the make, the model, the year, the category, the transmission and the branch.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the request.</param>
    [HttpPut("{vehicleId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<IActionResult> UpdateVehicle(Guid vehicleId, UpdateVehicleCommand command, CancellationToken cancellationToken) =>
        Respond(await dispatcher.Send(command with { VehicleId = vehicleId }, cancellationToken));

    /// <summary>Takes a vehicle out of use.</summary>
    /// <param name="vehicleId">The vehicle.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the request.</param>
    [HttpPost("{vehicleId:guid}/deactivate")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<IActionResult> Deactivate(Guid vehicleId, CancellationToken cancellationToken) =>
        Respond(await dispatcher.Send(new DeactivateVehicleCommand(vehicleId), cancellationToken));

    /// <summary>Puts a vehicle back in use.</summary>
    /// <param name="vehicleId">The vehicle.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the request.</param>
    [HttpPost("{vehicleId:guid}/reactivate")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<IActionResult> Reactivate(Guid vehicleId, CancellationToken cancellationToken) =>
        Respond(await dispatcher.Send(new ReactivateVehicleCommand(vehicleId), cancellationToken));
}
