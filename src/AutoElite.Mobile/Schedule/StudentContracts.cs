using System.Globalization;
using AutoElite.Client.Api;
using AutoElite.Client.Api.Models;
using AutoElite.Client.Offline;

namespace AutoElite.Mobile.Schedule;

/// <summary>
/// The contracts of the signed-in student with the progress of their training, and their payments, read from the API
/// or, without a network, from the device.
/// </summary>
/// <param name="cache">What the user last read.</param>
/// <param name="createApi">Creates the client of the API.</param>
public sealed class StudentContracts(OfflineCache cache, Func<AutoEliteApiClient> createApi)
{
    /// <summary>Reads the contracts of the student, active and closed.</summary>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    /// <returns>The contracts.</returns>
    public Task<Cached<IReadOnlyList<ContractListItem>>> ReadContractsAsync(CancellationToken cancellationToken) =>
        cache.GetListAsync(
            "me/contracts",
            token => createApi().Api.V1.Me.Contracts.GetAsync(cancellationToken: token),
            ContractListItem.CreateFromDiscriminatorValue,
            cancellationToken);

    /// <summary>Reads the card of a contract with the progress of the training.</summary>
    /// <param name="contractId">The contract.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    /// <returns>The card.</returns>
    public Task<Cached<ContractDetails>> ReadDetailsAsync(Guid contractId, CancellationToken cancellationToken) =>
        cache.GetAsync(
            string.Create(CultureInfo.InvariantCulture, $"contracts/{contractId:N}"),
            token => createApi().Api.V1.Contracts[contractId].GetAsync(cancellationToken: token),
            ContractDetails.CreateFromDiscriminatorValue,
            cancellationToken);

    /// <summary>Reads the payments of the student across their contracts, the latest first.</summary>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    /// <returns>The payments.</returns>
    public Task<Cached<IReadOnlyList<PaymentItem>>> ReadPaymentsAsync(CancellationToken cancellationToken) =>
        cache.GetListAsync(
            "me/payments",
            token => createApi().Api.V1.Me.Payments.GetAsync(cancellationToken: token),
            PaymentItem.CreateFromDiscriminatorValue,
            cancellationToken);
}
