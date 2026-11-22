using System.Globalization;
using AutoElite.Client.Api.Models;
using AutoElite.Mobile.Localization;
using AutoElite.Mobile.Schedule;
using AutoElite.Mobile.Session;
using Microsoft.Kiota.Abstractions;

namespace AutoElite.Mobile.Pages;

/// <summary>
/// The leads of a manager: the new ones of their branch scope, which anyone may take on, and those they are working on,
/// with how many of them their limit allows, read from the API or, without a network, from the device; each opens the
/// lead.
/// </summary>
/// <param name="leads">The leads of the school.</param>
/// <param name="session">The user of the application, whose leads are theirs.</param>
public sealed class LeadsViewModel(LeadBook leads, UserSession session) : ViewModel
{
    private Command? _refresh;

    /// <summary>Gets the new leads.</summary>
    public IReadOnlyList<LeadEntry> NewLeads
    {
        get;
        private set
        {
            if (SetProperty(ref field, value))
                OnPropertyChanged(nameof(HasNewLeads));
        }
    } = [];

    /// <summary>Gets a value indicating whether there are new leads.</summary>
    public bool HasNewLeads => NewLeads.Count > 0;

    /// <summary>Gets the leads the user is working on.</summary>
    public IReadOnlyList<LeadEntry> MyLeads
    {
        get;
        private set
        {
            if (SetProperty(ref field, value))
                OnPropertyChanged(nameof(HasMyLeads));
        }
    } = [];

    /// <summary>Gets a value indicating whether the user is working on leads.</summary>
    public bool HasMyLeads => MyLeads.Count > 0;

    /// <summary>Gets the heading of the leads the user is working on, with the number their limit allows.</summary>
    public string MyLeadsTitle
    {
        get;
        private set => SetProperty(ref field, value);
    } = AppTexts.Get("Leads.Mine");

    /// <summary>Gets what the page says when it has nothing to show, or <see langword="null"/>.</summary>
    public string? EmptyText
    {
        get;
        private set => SetProperty(ref field, value);
    }

    /// <summary>Gets that the leads are from the device, or why they could not be read.</summary>
    public string? Notice
    {
        get;
        private set
        {
            if (SetProperty(ref field, value))
                OnPropertyChanged(nameof(HasNotice));
        }
    }

    /// <summary>Gets a value indicating whether there is a notice to show.</summary>
    public bool HasNotice => Notice is not null;

    /// <summary>Gets or sets a value indicating whether the page is pulled down to read the leads again.</summary>
    public bool IsRefreshing
    {
        get;
        set => SetProperty(ref field, value);
    }

    /// <summary>Gets the command that reads the leads again.</summary>
    public Command RefreshCommand => _refresh ??= new Command(async () => await RefreshAsync());

    /// <summary>Reads the leads.</summary>
    /// <returns>A task that completes when the page shows them.</returns>
    public async Task LoadAsync()
    {
        try
        {
            var fresh = await leads.ReadNewAsync(CancellationToken.None);
            var userId = (await session.GetProfileAsync(CancellationToken.None)).Value.Id;
            var mine = userId is { } managerId ? await leads.ReadInProgressAsync(managerId, CancellationToken.None) : null;
            NewLeads = [.. fresh.Value.Select(Entry).OfType<LeadEntry>()];
            MyLeads = [.. (mine?.Value ?? []).Select(Entry).OfType<LeadEntry>()];
            MyLeadsTitle = await TitleOfMineAsync();
            DateTimeOffset? savedAt = fresh.IsFromCache ? fresh.SavedAt : mine is { IsFromCache: true } ? mine.SavedAt : null;
            Notice = savedAt is { } saved
                ? AppTexts.Format("Leads.Offline", saved.ToLocalTime().ToString("g", CultureInfo.CurrentCulture))
                : null;
            EmptyText = NewLeads.Count + MyLeads.Count == 0 ? AppTexts.Get("Leads.Empty") : null;
        }
        catch (Exception exception) when (exception is ApiException or HttpRequestException)
        {
            Notice = ProblemTexts.Of(exception);
        }
    }

    private static LeadEntry? Entry(LeadListItem lead)
    {
        if (lead.Id is not { } leadId)
            return null;

        var name = lead.ContactName ?? string.Empty;

        return new LeadEntry
        {
            Name = name,
            Contacts = LessonTexts.DetailsOf(lead.Phone, lead.Email),
            Details = LeadTexts.DetailsOf(lead.Source, lead.DesiredCategory, lead.CreatedAtUtc),
            OpenCommand = new Command(async () => await Routes.OpenAsync(
                Routes.Lead, new Dictionary<string, object> { [LeadViewModel.LeadParameter] = new LeadRequest(leadId, name) }))
        };
    }

    // The number in progress may be above the limit when an administrator gave the user more leads.
    private async Task<string> TitleOfMineAsync()
    {
        try
        {
            var workload = (await leads.ReadWorkloadAsync(CancellationToken.None)).Value;

            return AppTexts.Format("Leads.MineOf", workload.InProgress ?? 0, workload.Limit ?? 0);
        }
        catch (Exception exception) when (exception is ApiException or HttpRequestException)
        {
            return AppTexts.Get("Leads.Mine");
        }
    }

    private async Task RefreshAsync()
    {
        try
        {
            await LoadAsync();
        }
        finally
        {
            IsRefreshing = false;
        }
    }
}
