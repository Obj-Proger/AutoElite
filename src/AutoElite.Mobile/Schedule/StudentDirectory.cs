using System.Globalization;
using AutoElite.Client.Api;
using AutoElite.Client.Api.Models;
using AutoElite.Client.Offline;

namespace AutoElite.Mobile.Schedule;

/// <summary>
/// The students of the school as a manager or an administrator works with them: found by a part of the name, the email
/// address or the phone number, with the card of a student, their contracts with the payments, and the actions of the
/// office: booking a driving slot for a student, recording a payment and confirming one. The card and the payments are
/// read from the API or, without a network, from the device; the search and the actions need the network.
/// </summary>
/// <param name="cache">What the user last read.</param>
/// <param name="createApi">Creates the client of the API.</param>
public sealed class StudentDirectory(OfflineCache cache, Func<AutoEliteApiClient> createApi)
{
    // The students shown for a search: enough to find one, few enough to read at once.
    private const int PageSize = 30;

    /// <summary>Finds the active students of the user's branch scope, sorted by name.</summary>
    /// <param name="search">A part of the name, the email address or the phone number; empty for the first students.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    /// <returns>The students found.</returns>
    public async Task<IReadOnlyList<UserListItem>> SearchAsync(string search, CancellationToken cancellationToken)
    {
        var text = search.Trim();
        var page = await createApi().Api.V1.Users.GetAsync(
            request =>
            {
                request.QueryParameters.Role = UserRole.Student;
                request.QueryParameters.IsActive = true;
                request.QueryParameters.Search = text.Length == 0 ? null : text;
                request.QueryParameters.PageSize = PageSize;
            },
            cancellationToken);

        return [.. (page?.Items ?? []).OfType<UserListItem>()];
    }

    /// <summary>Reads the card of a student: who they are and each of their contracts with its money and progress.</summary>
    /// <param name="studentId">The student.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    /// <returns>The card.</returns>
    public Task<Cached<StudentCard>> ReadCardAsync(Guid studentId, CancellationToken cancellationToken) =>
        cache.GetAsync(
            string.Create(CultureInfo.InvariantCulture, $"students/{studentId:N}/card"),
            token => createApi().Api.V1.Students[studentId].Card.GetAsync(cancellationToken: token),
            StudentCard.CreateFromDiscriminatorValue,
            cancellationToken);

    /// <summary>Reads the payments of a contract, the latest first.</summary>
    /// <param name="contractId">The contract.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    /// <returns>The payments.</returns>
    public Task<Cached<IReadOnlyList<PaymentItem>>> ReadPaymentsAsync(Guid contractId, CancellationToken cancellationToken) =>
        cache.GetListAsync(
            string.Create(CultureInfo.InvariantCulture, $"contracts/{contractId:N}/payments"),
            token => createApi().Api.V1.Contracts[contractId].Payments.GetAsync(cancellationToken: token),
            PaymentItem.CreateFromDiscriminatorValue,
            cancellationToken);

    /// <summary>Books a free driving slot for a contract of a student.</summary>
    /// <param name="slotId">The slot.</param>
    /// <param name="contractId">The contract, which must fit the vehicle of the slot.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    /// <returns>A task that completes when the slot is booked.</returns>
    public Task BookAsync(Guid slotId, Guid contractId, CancellationToken cancellationToken) =>
        createApi().Api.V1.DrivingSlots[slotId].Book.PostAsync(
            new BookDrivingSlotCommand { SlotId = slotId, ContractId = contractId }, cancellationToken: cancellationToken);

    /// <summary>Records a payment towards a contract, confirmed at once when the money is received.</summary>
    /// <param name="contractId">The contract.</param>
    /// <param name="amount">The amount in the currency of the contract.</param>
    /// <param name="method">How the student paid.</param>
    /// <param name="purpose">What the payment is for.</param>
    /// <param name="isReceived">Whether the money is received, so that the payment counts towards the debt at once.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    /// <returns>A task that completes when the payment is recorded.</returns>
    public Task RegisterPaymentAsync(
        Guid contractId, double amount, PaymentMethod method, string purpose, bool isReceived, CancellationToken cancellationToken) =>
        createApi().Api.V1.Contracts[contractId].Payments.PostAsync(
            new RegisterPaymentCommand
            {
                ContractId = contractId,
                Amount = amount,
                Method = method,
                Purpose = purpose,
                ConfirmNow = isReceived
            },
            cancellationToken: cancellationToken);

    /// <summary>Confirms a recorded payment, whose money is now received.</summary>
    /// <param name="paymentId">The payment.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    /// <returns>A task that completes when the payment is confirmed.</returns>
    public Task ConfirmPaymentAsync(Guid paymentId, CancellationToken cancellationToken) =>
        createApi().Api.V1.Payments[paymentId].Confirm.PostAsync(cancellationToken: cancellationToken);
}
