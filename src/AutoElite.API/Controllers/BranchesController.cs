using AutoElite.API.Common;
using AutoElite.Application.Branches;
using AutoElite.Application.Branches.CreateBranch;
using AutoElite.Application.Branches.DeactivateBranch;
using AutoElite.Application.Branches.GetBranch;
using AutoElite.Application.Branches.GetBranches;
using AutoElite.Application.Branches.ReactivateBranch;
using AutoElite.Application.Branches.UpdateBranch;
using Microsoft.AspNetCore.Mvc;

namespace AutoElite.API.Controllers;

/// <summary>The branches of the school (FR-1.8): read by every signed-in user, managed by administrators.</summary>
/// <param name="dispatcher">The dispatcher of commands and queries.</param>
[Route(ApiRoutes.Prefix + "/branches")]
public sealed class BranchesController(IDispatcher dispatcher) : ApiControllerBase
{
    /// <summary>Lists the branches by name.</summary>
    /// <param name="isActive">Whether the branches are active; all when not given.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the request.</param>
    /// <returns>The branches.</returns>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<BranchItem>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetBranches(bool? isActive, CancellationToken cancellationToken) =>
        Respond(await dispatcher.Send(new GetBranchesQuery(isActive), cancellationToken));

    /// <summary>Reads a branch.</summary>
    /// <param name="branchId">The branch.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the request.</param>
    /// <returns>The branch.</returns>
    [HttpGet("{branchId:guid}")]
    [ProducesResponseType<BranchItem>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<IActionResult> GetBranch(Guid branchId, CancellationToken cancellationToken) =>
        Respond(await dispatcher.Send(new GetBranchQuery(branchId), cancellationToken));

    /// <summary>Opens a branch.</summary>
    /// <param name="command">The name, the address, the phone and the time zone of the branch.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the request.</param>
    /// <returns>The identifier of the branch.</returns>
    [HttpPost]
    [ProducesResponseType<CreatedResource>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<IActionResult> CreateBranch(CreateBranchCommand command, CancellationToken cancellationToken) =>
        RespondCreated(await dispatcher.Send(command, cancellationToken), nameof(GetBranch), "branchId");

    /// <summary>Changes a branch.</summary>
    /// <param name="branchId">The branch; it takes the place of the one in the body.</param>
    /// <param name="command">The name, the address, the phone and the time zone of the branch.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the request.</param>
    [HttpPut("{branchId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<IActionResult> UpdateBranch(Guid branchId, UpdateBranchCommand command, CancellationToken cancellationToken) =>
        Respond(await dispatcher.Send(command with { BranchId = branchId }, cancellationToken));

    /// <summary>Closes a branch; its history stays.</summary>
    /// <param name="branchId">The branch.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the request.</param>
    [HttpPost("{branchId:guid}/deactivate")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<IActionResult> Deactivate(Guid branchId, CancellationToken cancellationToken) =>
        Respond(await dispatcher.Send(new DeactivateBranchCommand(branchId), cancellationToken));

    /// <summary>Opens a closed branch again.</summary>
    /// <param name="branchId">The branch.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the request.</param>
    [HttpPost("{branchId:guid}/reactivate")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<IActionResult> Reactivate(Guid branchId, CancellationToken cancellationToken) =>
        Respond(await dispatcher.Send(new ReactivateBranchCommand(branchId), cancellationToken));
}
