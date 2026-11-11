using AutoElite.API.Common;
using AutoElite.Application.Common.Pagination;
using AutoElite.Application.Contracts;
using AutoElite.Application.Contracts.ChangeContractPrice;
using AutoElite.Application.Contracts.ChangeContractTransmission;
using AutoElite.Application.Contracts.CompleteContract;
using AutoElite.Application.Contracts.DrawUpContract;
using AutoElite.Application.Contracts.GetContract;
using AutoElite.Application.Contracts.GetContracts;
using AutoElite.Application.Contracts.GetMyContracts;
using AutoElite.Application.Contracts.RescheduleContract;
using AutoElite.Application.Contracts.TerminateContract;
using Microsoft.AspNetCore.Mvc;

namespace AutoElite.API.Controllers;

/// <summary>
/// The training contracts: drawn up and changed by managers and administrators, read by their students. A manager
/// works with the contracts of their own branch.
/// </summary>
/// <param name="dispatcher">The dispatcher of commands and queries.</param>
[Route(ApiRoutes.Prefix + "/contracts")]
public sealed class ContractsController(IDispatcher dispatcher) : ApiControllerBase
{
    /// <summary>Finds contracts, a page at a time.</summary>
    /// <param name="search">Text in the number, the student's name or email address.</param>
    /// <param name="status">The status of the contracts.</param>
    /// <param name="courseId">The course.</param>
    /// <param name="branchId">The branch.</param>
    /// <param name="studentId">The student.</param>
    /// <param name="hasDebt">Whether the student owes money on the contracts.</param>
    /// <param name="page">The page, from 1.</param>
    /// <param name="pageSize">The number of contracts on a page.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the request.</param>
    /// <returns>The page of contracts.</returns>
    [HttpGet]
    [ProducesResponseType<PagedList<ContractListItem>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetContracts(
        string? search,
        ContractStatus? status,
        Guid? courseId,
        Guid? branchId,
        Guid? studentId,
        bool? hasDebt,
        int page = 1,
        int pageSize = PageRequest.DefaultPageSize,
        CancellationToken cancellationToken = default) =>
        Respond(await dispatcher.Send(
            new GetContractsQuery(search, status, courseId, branchId, studentId, hasDebt, page, pageSize), cancellationToken));

    /// <summary>Lists the contracts of the signed-in student.</summary>
    /// <param name="cancellationToken">A token that signals cancellation of the request.</param>
    /// <returns>The contracts.</returns>
    [HttpGet("/" + ApiRoutes.Prefix + "/me/contracts")]
    [ProducesResponseType<IReadOnlyList<ContractListItem>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyContracts(CancellationToken cancellationToken) =>
        Respond(await dispatcher.Send(new GetMyContractsQuery(), cancellationToken));

    /// <summary>Reads a contract; a student reads only their own.</summary>
    /// <param name="contractId">The contract.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the request.</param>
    /// <returns>The contract.</returns>
    [HttpGet("{contractId:guid}")]
    [ProducesResponseType<ContractDetails>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<IActionResult> GetContract(Guid contractId, CancellationToken cancellationToken) =>
        Respond(await dispatcher.Send(new GetContractQuery(contractId), cancellationToken));

    /// <summary>
    /// Draws up a contract for a registered student; a price that is not given is the price of the course, and a number
    /// that is not given is made by the school's sequence.
    /// </summary>
    /// <param name="command">The student, the course and the terms.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the request.</param>
    /// <returns>The identifier of the contract.</returns>
    [HttpPost]
    [ProducesResponseType<CreatedResource>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<IActionResult> DrawUpContract(DrawUpContractCommand command, CancellationToken cancellationToken) =>
        RespondCreated(await dispatcher.Send(command, cancellationToken), nameof(GetContract), "contractId");

    /// <summary>Changes the price of a contract.</summary>
    /// <param name="contractId">The contract; it takes the place of the one in the body.</param>
    /// <param name="command">The new price, in the currency of the contract.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the request.</param>
    [HttpPut("{contractId:guid}/price")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<IActionResult> ChangePrice(
        Guid contractId, ChangeContractPriceCommand command, CancellationToken cancellationToken) =>
        Respond(await dispatcher.Send(command with { ContractId = contractId }, cancellationToken));

    /// <summary>Changes the transmission the student learns to drive.</summary>
    /// <param name="contractId">The contract; it takes the place of the one in the body.</param>
    /// <param name="command">The transmission.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the request.</param>
    [HttpPut("{contractId:guid}/transmission")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<IActionResult> ChangeTransmission(
        Guid contractId, ChangeContractTransmissionCommand command, CancellationToken cancellationToken) =>
        Respond(await dispatcher.Send(command with { ContractId = contractId }, cancellationToken));

    /// <summary>Changes the dates of a contract.</summary>
    /// <param name="contractId">The contract; it takes the place of the one in the body.</param>
    /// <param name="command">The start and the end date.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the request.</param>
    [HttpPut("{contractId:guid}/dates")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<IActionResult> Reschedule(Guid contractId, RescheduleContractCommand command, CancellationToken cancellationToken) =>
        Respond(await dispatcher.Send(command with { ContractId = contractId }, cancellationToken));

    /// <summary>
    /// Completes a contract: the student has finished the training. Bookings of lessons that have not started are
    /// cancelled and the student leaves their study groups.
    /// </summary>
    /// <param name="contractId">The contract; it takes the place of the one in the body.</param>
    /// <param name="command">The date of completion.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the request.</param>
    [HttpPost("{contractId:guid}/complete")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<IActionResult> Complete(Guid contractId, CompleteContractCommand command, CancellationToken cancellationToken) =>
        Respond(await dispatcher.Send(command with { ContractId = contractId }, cancellationToken));

    /// <summary>
    /// Terminates a contract early, with the reason. As when a contract is completed, the student's bookings of lessons
    /// that have not started are cancelled and the student leaves their study groups.
    /// </summary>
    /// <param name="contractId">The contract; it takes the place of the one in the body.</param>
    /// <param name="command">The reason and the date of termination.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the request.</param>
    [HttpPost("{contractId:guid}/terminate")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<IActionResult> Terminate(Guid contractId, TerminateContractCommand command, CancellationToken cancellationToken) =>
        Respond(await dispatcher.Send(command with { ContractId = contractId }, cancellationToken));
}
