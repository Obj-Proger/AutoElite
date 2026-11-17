using System.Globalization;
using AutoElite.Client.Api;
using AutoElite.Client.Api.Models;
using AutoElite.Client.Offline;

namespace AutoElite.Mobile.Schedule;

/// <summary>
/// The leads of the school as a manager works with them in the application: the new ones of their branch scope and
/// those they are working on, a lead with its notes, read from the API or, without a network, from the device; and the
/// actions of the funnel short of a contract, which is drawn up on the website: taking a lead on, writing a note, marking
/// it lost and opening it again. The actions need the network.
/// </summary>
/// <param name="cache">What the user last read.</param>
/// <param name="createApi">Creates the client of the API.</param>
public sealed class LeadBook(OfflineCache cache, Func<AutoEliteApiClient> createApi)
{
    // The leads read at once: more than a manager works on at a time.
    private const int PageSize = 100;

    /// <summary>Reads the new leads of the user's branch scope, the newest first.</summary>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    /// <returns>The leads.</returns>
    public Task<Cached<IReadOnlyList<LeadListItem>>> ReadNewAsync(CancellationToken cancellationToken) =>
        ReadAsync("leads/new", LeadStatus.New, null, cancellationToken);

    /// <summary>Reads the leads a manager is working on, the newest first.</summary>
    /// <param name="managerId">The manager.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    /// <returns>The leads.</returns>
    public Task<Cached<IReadOnlyList<LeadListItem>>> ReadInProgressAsync(Guid managerId, CancellationToken cancellationToken) =>
        ReadAsync("leads/mine", LeadStatus.InProgress, managerId, cancellationToken);

    /// <summary>Reads a lead with its notes.</summary>
    /// <param name="leadId">The lead.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    /// <returns>The lead.</returns>
    public Task<Cached<LeadDetails>> ReadAsync(Guid leadId, CancellationToken cancellationToken) =>
        cache.GetAsync(
            string.Create(CultureInfo.InvariantCulture, $"leads/{leadId:N}"),
            token => createApi().Api.V1.Leads[leadId].GetAsync(cancellationToken: token),
            LeadDetails.CreateFromDiscriminatorValue,
            cancellationToken);

    /// <summary>Makes a manager responsible for a lead.</summary>
    /// <param name="leadId">The lead.</param>
    /// <param name="managerId">The manager.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    /// <returns>A task that completes when the manager is assigned.</returns>
    public Task AssignAsync(Guid leadId, Guid managerId, CancellationToken cancellationToken) =>
        createApi().Api.V1.Leads[leadId].Manager.PutAsync(
            new AssignLeadManagerCommand { LeadId = leadId, ManagerId = managerId }, cancellationToken: cancellationToken);

    /// <summary>Starts the work on a new lead.</summary>
    /// <param name="leadId">The lead.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    /// <returns>A task that completes when the lead is in progress.</returns>
    public Task StartAsync(Guid leadId, CancellationToken cancellationToken) =>
        createApi().Api.V1.Leads[leadId].StartWork.PostAsync(cancellationToken: cancellationToken);

    /// <summary>Writes a note on a lead, such as what was said on the phone.</summary>
    /// <param name="leadId">The lead.</param>
    /// <param name="text">The note.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    /// <returns>A task that completes when the note is written.</returns>
    public Task AddNoteAsync(Guid leadId, string text, CancellationToken cancellationToken) =>
        createApi().Api.V1.Leads[leadId].Notes.PostAsync(
            new AddLeadNoteCommand { LeadId = leadId, Text = text }, cancellationToken: cancellationToken);

    /// <summary>Marks an open lead lost, with the reason.</summary>
    /// <param name="leadId">The lead.</param>
    /// <param name="reason">Why the lead was lost.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    /// <returns>A task that completes when the lead is lost.</returns>
    public Task MarkLostAsync(Guid leadId, string reason, CancellationToken cancellationToken) =>
        createApi().Api.V1.Leads[leadId].Lost.PostAsync(
            new MarkLeadLostCommand { LeadId = leadId, Reason = reason }, cancellationToken: cancellationToken);

    /// <summary>Opens a lost lead again.</summary>
    /// <param name="leadId">The lead.</param>
    /// <param name="cancellationToken">A token that signals cancellation of the operation.</param>
    /// <returns>A task that completes when the lead is in progress again.</returns>
    public Task ReopenAsync(Guid leadId, CancellationToken cancellationToken) =>
        createApi().Api.V1.Leads[leadId].Reopen.PostAsync(cancellationToken: cancellationToken);

    private Task<Cached<IReadOnlyList<LeadListItem>>> ReadAsync(
        string key, LeadStatus status, Guid? managerId, CancellationToken cancellationToken) =>
        cache.GetListAsync(
            key,
            async token => (await createApi().Api.V1.Leads.GetAsync(
                request =>
                {
                    request.QueryParameters.Status = status;
                    request.QueryParameters.ManagerId = managerId;
                    request.QueryParameters.PageSize = PageSize;
                },
                token))?.Items,
            LeadListItem.CreateFromDiscriminatorValue,
            cancellationToken);
}
