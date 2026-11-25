using AutoElite.Client.Api.Models;
using AutoElite.Mobile.Localization;
using AutoElite.Mobile.Schedule;
using Microsoft.Kiota.Abstractions;

namespace AutoElite.Mobile.Pages;

/// <summary>
/// A lead that a manager records while the person is still on the phone or at the desk: the name, the phone number as
/// it is dictated, an email address if the person prefers it, where they came from, the licence category they want if
/// they know it, and what they asked. The lead goes to the manager, who takes it on at once; its page opens in place of
/// this one. Recording needs the network.
/// </summary>
public sealed class NewLeadViewModel : ViewModel
{
    // The channels a manager records a lead from; the website form records its own leads.
    private static readonly LeadSource[] _sources =
        [LeadSource.PhoneCall, LeadSource.WalkIn, LeadSource.Referral, LeadSource.SocialMedia, LeadSource.Advertising];

    private static readonly LicenseCategory[] _categories = Enum.GetValues<LicenseCategory>();

    private readonly LeadBook _leads;

    /// <summary>Initializes a new instance of the <see cref="NewLeadViewModel"/> class.</summary>
    /// <param name="leads">The leads of the school.</param>
    public NewLeadViewModel(LeadBook leads)
    {
        _leads = leads;
        SaveCommand = new Command(async () => await SaveAsync(), () => !IsBusy);
    }

    /// <summary>Gets or sets the name the person gave.</summary>
    public string Name
    {
        get;
        set => SetProperty(ref field, value);
    } = string.Empty;

    /// <summary>Gets or sets the phone number as typed.</summary>
    public string Phone
    {
        get;
        set => SetProperty(ref field, value);
    } = string.Empty;

    /// <summary>Gets or sets the email address, if the person gave one.</summary>
    public string Email
    {
        get;
        set => SetProperty(ref field, value);
    } = string.Empty;

    /// <summary>Gets the channels a lead may come from, in the language of the user.</summary>
    public IReadOnlyList<string> Sources { get; } = [.. _sources.Select(source => LeadTexts.SourceOf(source) ?? source.ToString())];

    /// <summary>Gets or sets the position of the channel chosen: a phone call unless the manager chooses another.</summary>
    public int SourceIndex
    {
        get;
        set => SetProperty(ref field, value);
    }

    /// <summary>Gets the licence categories, after the choice that the person does not know yet.</summary>
    public IReadOnlyList<string> Categories { get; } =
        [AppTexts.Get("NewLead.AnyCategory"), .. _categories.Select(CategoryNames.Of)];

    /// <summary>Gets or sets the position of the category chosen; the first one leaves it open.</summary>
    public int CategoryIndex
    {
        get;
        set => SetProperty(ref field, value);
    }

    /// <summary>Gets or sets what the person asked, such as when to call back.</summary>
    public string Comment
    {
        get;
        set => SetProperty(ref field, value);
    } = string.Empty;

    /// <summary>Gets why the lead cannot be recorded.</summary>
    public string? Message
    {
        get;
        private set => SetProperty(ref field, value);
    }

    /// <summary>Gets a value indicating whether the lead is being recorded.</summary>
    public bool IsBusy
    {
        get;
        private set
        {
            if (SetProperty(ref field, value))
                SaveCommand.ChangeCanExecute();
        }
    }

    /// <summary>Gets the command that records the lead and opens it.</summary>
    public Command SaveCommand { get; }

    private static string? Optional(string text) => string.IsNullOrWhiteSpace(text) ? null : text.Trim();

    private async Task SaveAsync()
    {
        var phone = PhoneNumbers.Normalize(Phone);
        var email = Optional(Email);
        if (string.IsNullOrWhiteSpace(Name))
        {
            Message = AppTexts.Get("NewLead.NoName");

            return;
        }

        if (phone.Length == 0 && email is null)
        {
            Message = AppTexts.Get("NewLead.NoContact");

            return;
        }

        IsBusy = true;
        Message = null;
        try
        {
            var name = Name.Trim();
            var leadId = await _leads.CreateAsync(
                new CreateLeadCommand
                {
                    ContactName = name,
                    Phone = phone.Length == 0 ? null : phone,
                    Email = email,
                    Source = _sources[Math.Clamp(SourceIndex, 0, _sources.Length - 1)],
                    DesiredCategory = CategoryIndex > 0 ? _categories[Math.Min(CategoryIndex - 1, _categories.Length - 1)] : null,
                    Comment = Optional(Comment)
                },
                CancellationToken.None);
            await TakeOnAsync(leadId);
            await Routes.ReplaceAsync(
                Routes.Lead, new Dictionary<string, object> { [LeadViewModel.LeadParameter] = new LeadRequest(leadId, name) });
        }
        catch (Exception exception) when (exception is ApiException or HttpRequestException or TaskCanceledException)
        {
            Message = ProblemTexts.Of(exception);
        }
        finally
        {
            IsBusy = false;
        }
    }

    // The manager is talking to the person, so the lead is in progress at once; when their limit does not allow another
    // lead, it stays new and its page offers to take it on later.
    private async Task TakeOnAsync(Guid leadId)
    {
        try
        {
            await _leads.StartAsync(leadId, CancellationToken.None);
        }
        catch (Exception exception) when (exception is ApiException or HttpRequestException or TaskCanceledException)
        {
            // The lead is recorded; taking it on is left to its page.
        }
    }
}
