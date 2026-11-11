using AutoElite.API.Common;
using AutoElite.Application.Common.Pagination;
using AutoElite.Application.Payments;
using AutoElite.Application.Payments.CancelPayment;
using AutoElite.Application.Payments.ConfirmPayment;
using AutoElite.Application.Payments.GetContractPayments;
using AutoElite.Application.Payments.GetMyPayments;
using AutoElite.Application.Payments.GetPayments;
using AutoElite.Application.Payments.RefundPayment;
using AutoElite.Application.Payments.RegisterPayment;
using AutoElite.Application.Payments.SetPaymentReference;
using Microsoft.AspNetCore.Mvc;

namespace AutoElite.API.Controllers;

/// <summary>
/// The payments towards contracts: recorded, confirmed, refunded and cancelled by managers and administrators, read by
/// students for their own contracts. A payment counts towards the debt of its contract only while it is confirmed, and
/// payments are never deleted.
/// </summary>
/// <param name="dispatcher">The dispatcher of commands and queries.</param>
[Route(ApiRoutes.Prefix + "/payments")]
public sealed class PaymentsController(IDispatcher dispatcher) : ApiControllerBase
{
    /// <summary>Finds the payments of a period, the latest first, a page at a time, with the totals per currency.</summary>
    /// <param name="from">The first UTC day of the period.</param>
    /// <param name="to">The last UTC day of the period.</param>
    /// <param name="method">How the payments were made.</param>
    /// <param name="status">The status of the payments, for example confirmed ones for the income.</param>
    /// <param name="registeredByUserId">The staff member who recorded the payments.</param>
    /// <param name="branchId">The branch of the contracts.</param>
    /// <param name="search">Text in the contract number or the student's name.</param>
    /// <param name="page">The page, from 1.</param>
    /// <param name="pageSize">The number of payments on a page.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the request.</param>
    /// <returns>The page of payments and the totals of all payments found.</returns>
    [HttpGet]
    [ProducesResponseType<PaymentReport>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPayments(
        DateOnly? from,
        DateOnly? to,
        PaymentMethod? method,
        PaymentStatus? status,
        Guid? registeredByUserId,
        Guid? branchId,
        string? search,
        int page = 1,
        int pageSize = PageRequest.DefaultPageSize,
        CancellationToken cancellationToken = default) =>
        Respond(await dispatcher.Send(
            new GetPaymentsQuery(from, to, method, status, registeredByUserId, branchId, search, page, pageSize),
            cancellationToken));

    /// <summary>Lists the payments of the signed-in student across all of their contracts, the latest first.</summary>
    /// <param name="cancellationToken">A token that signals cancellation of the request.</param>
    /// <returns>The payments.</returns>
    [HttpGet("/" + ApiRoutes.Prefix + "/me/payments")]
    [ProducesResponseType<IReadOnlyList<PaymentItem>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyPayments(CancellationToken cancellationToken) =>
        Respond(await dispatcher.Send(new GetMyPaymentsQuery(), cancellationToken));

    /// <summary>Lists the payment history of a contract; a student reads only their own contracts.</summary>
    /// <param name="contractId">The contract.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the request.</param>
    /// <returns>The payments.</returns>
    [HttpGet("/" + ApiRoutes.Prefix + "/contracts/{contractId:guid}/payments")]
    [ProducesResponseType<IReadOnlyList<PaymentItem>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<IActionResult> GetContractPayments(Guid contractId, CancellationToken cancellationToken) =>
        Respond(await dispatcher.Send(new GetContractPaymentsQuery(contractId), cancellationToken));

    /// <summary>
    /// Records a payment towards a contract in the currency of the contract; money already received can be confirmed at
    /// once. The Location header leads to the payments of the contract.
    /// </summary>
    /// <param name="contractId">The contract; it takes the place of the one in the body.</param>
    /// <param name="command">The amount, the method, the purpose, the reference and whether to confirm it now.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the request.</param>
    /// <returns>The identifier of the payment.</returns>
    [HttpPost("/" + ApiRoutes.Prefix + "/contracts/{contractId:guid}/payments")]
    [ProducesResponseType<CreatedResource>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<IActionResult> RegisterPayment(
        Guid contractId, RegisterPaymentCommand command, CancellationToken cancellationToken) =>
        RespondCreatedIn(
            await dispatcher.Send(command with { ContractId = contractId }, cancellationToken),
            nameof(GetContractPayments),
            new { contractId });

    /// <summary>
    /// Confirms that the money of a recorded payment has arrived: the amount counts towards the contract, and the student
    /// is notified.
    /// </summary>
    /// <param name="paymentId">The payment.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the request.</param>
    [HttpPost("{paymentId:guid}/confirm")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<IActionResult> Confirm(Guid paymentId, CancellationToken cancellationToken) =>
        Respond(await dispatcher.Send(new ConfirmPaymentCommand(paymentId), cancellationToken));

    /// <summary>Returns a confirmed payment in full, with the reason; the debt of the contract grows back.</summary>
    /// <param name="paymentId">The payment; it takes the place of the one in the body.</param>
    /// <param name="command">The reason.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the request.</param>
    [HttpPost("{paymentId:guid}/refund")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<IActionResult> Refund(Guid paymentId, RefundPaymentCommand command, CancellationToken cancellationToken) =>
        Respond(await dispatcher.Send(command with { PaymentId = paymentId }, cancellationToken));

    /// <summary>
    /// Cancels a recorded payment that was entered by mistake or never arrived, with the reason, which stays in the
    /// history.
    /// </summary>
    /// <param name="paymentId">The payment; it takes the place of the one in the body.</param>
    /// <param name="command">The reason.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the request.</param>
    [HttpPost("{paymentId:guid}/cancel")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<IActionResult> Cancel(Guid paymentId, CancelPaymentCommand command, CancellationToken cancellationToken) =>
        Respond(await dispatcher.Send(command with { PaymentId = paymentId }, cancellationToken));

    /// <summary>Records the bank or gateway identifier of a recorded payment, for example from a bank statement.</summary>
    /// <param name="paymentId">The payment; it takes the place of the one in the body.</param>
    /// <param name="command">The identifier in the bank or gateway.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the request.</param>
    [HttpPut("{paymentId:guid}/reference")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<IActionResult> SetReference(
        Guid paymentId, SetPaymentReferenceCommand command, CancellationToken cancellationToken) =>
        Respond(await dispatcher.Send(command with { PaymentId = paymentId }, cancellationToken));
}
