using AutoElite.Client.Api;
using AutoElite.Client.Api.Models;
using AutoElite.Client.Offline;

namespace AutoElite.Mobile.Schedule;

/// <summary>
/// The money of the school as the office follows it: the payments recorded but not confirmed yet, whose money is still
/// to come, and the active contracts whose students owe money, read from the API or, without a network, from the device;
/// and the actions on a waiting payment: confirming it once the money has come and cancelling one recorded by mistake.
/// The actions need the network.
/// </summary>
/// <param name="cache">What the user last read.</param>
/// <param name="createApi">Creates the client of the API.</param>
public sealed class OfficeMoney(OfflineCache cache, Func<AutoEliteApiClient> createApi)
{
    // The most the API gives at once; more waiting payments or debtors than that are left to the website.
    private const int PageSize = 100;

    /// <summary>Reads the payments that wait for their confirmation, the latest first.</summary>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    /// <returns>The payments.</returns>
    public Task<Cached<IReadOnlyList<PaymentItem>>> ReadWaitingAsync(CancellationToken cancellationToken) =>
        cache.GetListAsync(
            "payments/waiting",
            async token => (await createApi().Api.V1.Payments.GetAsync(
                request =>
                {
                    request.QueryParameters.Status = PaymentStatus.Registered;
                    request.QueryParameters.PageSize = PageSize;
                },
                token))?.Page?.Items,
            PaymentItem.CreateFromDiscriminatorValue,
            cancellationToken);

    /// <summary>Reads the active contracts whose students owe money.</summary>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    /// <returns>The contracts, in no particular order.</returns>
    public Task<Cached<IReadOnlyList<ContractListItem>>> ReadDebtsAsync(CancellationToken cancellationToken) =>
        cache.GetListAsync(
            "contracts/debts",
            async token => (await createApi().Api.V1.Contracts.GetAsync(
                request =>
                {
                    request.QueryParameters.Status = ContractStatus.Active;
                    request.QueryParameters.HasDebt = true;
                    request.QueryParameters.PageSize = PageSize;
                },
                token))?.Items,
            ContractListItem.CreateFromDiscriminatorValue,
            cancellationToken);

    /// <summary>Confirms a recorded payment, whose money is now received.</summary>
    /// <param name="paymentId">The payment.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    /// <returns>A task that completes when the payment is confirmed.</returns>
    public Task ConfirmAsync(Guid paymentId, CancellationToken cancellationToken) =>
        createApi().Api.V1.Payments[paymentId].Confirm.PostAsync(cancellationToken: cancellationToken);

    /// <summary>Cancels a recorded payment that was entered by mistake or never arrived; the reason stays in the history.</summary>
    /// <param name="paymentId">The payment.</param>
    /// <param name="reason">Why the payment is cancelled.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    /// <returns>A task that completes when the payment is cancelled.</returns>
    public Task CancelAsync(Guid paymentId, string reason, CancellationToken cancellationToken) =>
        createApi().Api.V1.Payments[paymentId].Cancel.PostAsync(
            new CancelPaymentCommand { PaymentId = paymentId, Reason = reason }, cancellationToken: cancellationToken);
}
