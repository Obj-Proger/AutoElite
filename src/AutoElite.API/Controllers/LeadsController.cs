using AutoElite.API.Common;
using AutoElite.Application.Common.Pagination;
using AutoElite.Application.Contracts.ConvertLeadToContract;
using AutoElite.Application.Leads;
using AutoElite.Application.Leads.AddLeadNote;
using AutoElite.Application.Leads.AssignLeadManager;
using AutoElite.Application.Leads.ChangeLeadBranch;
using AutoElite.Application.Leads.CreateLead;
using AutoElite.Application.Leads.GetLead;
using AutoElite.Application.Leads.GetLeads;
using AutoElite.Application.Leads.MarkLeadLost;
using AutoElite.Application.Leads.ReopenLead;
using AutoElite.Application.Leads.StartLeadWork;
using AutoElite.Application.Leads.SubmitLeadRequest;
using AutoElite.Application.Leads.UpdateLeadContact;
using Microsoft.AspNetCore.Mvc;

namespace AutoElite.API.Controllers;

/// <summary>
/// The leads of the school (FR-2.1..2.6): the public request form, and the work of managers and administrators with a
/// lead up to its contract. A manager works with the leads of their own branch.
/// </summary>
/// <param name="dispatcher">The dispatcher of commands and queries.</param>
[Route(ApiRoutes.Prefix + "/leads")]
public sealed class LeadsController(IDispatcher dispatcher) : ApiControllerBase
{
    /// <summary>
    /// Submits a request from the public form of the school's site, without signing in (FR-2.2); the managers of the
    /// branch are notified.
    /// </summary>
    /// <param name="command">The contact, the wished category, a comment and the branch.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the request.</param>
    [HttpPost("requests")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> SubmitRequest(SubmitLeadRequestCommand command, CancellationToken cancellationToken) =>
        Respond(await dispatcher.Send(command, cancellationToken));

    /// <summary>Finds leads, a page at a time.</summary>
    /// <param name="search">Text in the name, the phone or the email address.</param>
    /// <param name="status">The status of the leads.</param>
    /// <param name="source">Where the leads came from.</param>
    /// <param name="managerId">The responsible manager.</param>
    /// <param name="branchId">The branch.</param>
    /// <param name="page">The page, from 1.</param>
    /// <param name="pageSize">The number of leads on a page.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the request.</param>
    /// <returns>The page of leads.</returns>
    [HttpGet]
    [ProducesResponseType<PagedList<LeadListItem>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetLeads(
        string? search,
        LeadStatus? status,
        LeadSource? source,
        Guid? managerId,
        Guid? branchId,
        int page = 1,
        int pageSize = PageRequest.DefaultPageSize,
        CancellationToken cancellationToken = default) =>
        Respond(await dispatcher.Send(
            new GetLeadsQuery(search, status, source, managerId, branchId, page, pageSize), cancellationToken));

    /// <summary>Reads a lead with its notes and the history of its status.</summary>
    /// <param name="leadId">The lead.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the request.</param>
    /// <returns>The lead.</returns>
    [HttpGet("{leadId:guid}")]
    [ProducesResponseType<LeadDetails>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<IActionResult> GetLead(Guid leadId, CancellationToken cancellationToken) =>
        Respond(await dispatcher.Send(new GetLeadQuery(leadId), cancellationToken));

    /// <summary>Records a lead that came by phone, in person or another way.</summary>
    /// <param name="command">The contact, the source, the wished category, a comment, the branch and the manager.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the request.</param>
    /// <returns>The identifier of the lead.</returns>
    [HttpPost]
    [ProducesResponseType<CreatedResource>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<IActionResult> CreateLead(CreateLeadCommand command, CancellationToken cancellationToken) =>
        RespondCreated(await dispatcher.Send(command, cancellationToken), nameof(GetLead), "leadId");

    /// <summary>Changes the contact of a lead, the wished category and the comment.</summary>
    /// <param name="leadId">The lead; it takes the place of the one in the body.</param>
    /// <param name="command">The contact, the wished category and the comment.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the request.</param>
    [HttpPut("{leadId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<IActionResult> UpdateContact(Guid leadId, UpdateLeadContactCommand command, CancellationToken cancellationToken) =>
        Respond(await dispatcher.Send(command with { LeadId = leadId }, cancellationToken));

    /// <summary>Makes a manager responsible for a lead (FR-2.4).</summary>
    /// <param name="leadId">The lead; it takes the place of the one in the body.</param>
    /// <param name="command">The manager.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the request.</param>
    [HttpPut("{leadId:guid}/manager")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<IActionResult> AssignManager(Guid leadId, AssignLeadManagerCommand command, CancellationToken cancellationToken) =>
        Respond(await dispatcher.Send(command with { LeadId = leadId }, cancellationToken));

    /// <summary>Moves a lead to another branch, or takes it out of any.</summary>
    /// <param name="leadId">The lead; it takes the place of the one in the body.</param>
    /// <param name="command">The branch.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the request.</param>
    [HttpPut("{leadId:guid}/branch")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<IActionResult> ChangeBranch(Guid leadId, ChangeLeadBranchCommand command, CancellationToken cancellationToken) =>
        Respond(await dispatcher.Send(command with { LeadId = leadId }, cancellationToken));

    /// <summary>Starts working with a new lead.</summary>
    /// <param name="leadId">The lead.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the request.</param>
    [HttpPost("{leadId:guid}/start-work")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<IActionResult> StartWork(Guid leadId, CancellationToken cancellationToken) =>
        Respond(await dispatcher.Send(new StartLeadWorkCommand(leadId), cancellationToken));

    /// <summary>Marks a lead lost, with the reason.</summary>
    /// <param name="leadId">The lead; it takes the place of the one in the body.</param>
    /// <param name="command">The reason.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the request.</param>
    [HttpPost("{leadId:guid}/lost")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<IActionResult> MarkLost(Guid leadId, MarkLeadLostCommand command, CancellationToken cancellationToken) =>
        Respond(await dispatcher.Send(command with { LeadId = leadId }, cancellationToken));

    /// <summary>Returns a lost lead to work.</summary>
    /// <param name="leadId">The lead.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the request.</param>
    [HttpPost("{leadId:guid}/reopen")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<IActionResult> Reopen(Guid leadId, CancellationToken cancellationToken) =>
        Respond(await dispatcher.Send(new ReopenLeadCommand(leadId), cancellationToken));

    /// <summary>Adds a note to the journal of a lead (FR-2.5); the Location header leads to the lead.</summary>
    /// <param name="leadId">The lead; it takes the place of the one in the body.</param>
    /// <param name="command">The text of the note.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the request.</param>
    /// <returns>The identifier of the note.</returns>
    [HttpPost("{leadId:guid}/notes")]
    [ProducesResponseType<CreatedResource>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<IActionResult> AddNote(Guid leadId, AddLeadNoteCommand command, CancellationToken cancellationToken) =>
        RespondCreatedIn(
            await dispatcher.Send(command with { LeadId = leadId }, cancellationToken), nameof(GetLead), new { leadId });

    /// <summary>
    /// Converts a lead into a contract in one step (FR-2.6): registers the student, or takes an existing one, and draws
    /// up the contract with the branch of the lead. The Location header leads to the contract.
    /// </summary>
    /// <param name="leadId">The lead; it takes the place of the one in the body.</param>
    /// <param name="command">The student and the terms of the contract.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the request.</param>
    /// <returns>The student and the contract.</returns>
    [HttpPost("{leadId:guid}/conversion")]
    [ProducesResponseType<LeadConversion>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<IActionResult> ConvertToContract(
        Guid leadId, ConvertLeadToContractCommand command, CancellationToken cancellationToken) =>
        RespondCreatedAt(
            await dispatcher.Send(command with { LeadId = leadId }, cancellationToken),
            nameof(ContractsController.GetContract),
            "Contracts",
            conversion => new { contractId = conversion.ContractId });
}
