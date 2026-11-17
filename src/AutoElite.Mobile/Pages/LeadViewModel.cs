using System.Globalization;
using AutoElite.Client.Api.Models;
using AutoElite.Mobile.Localization;
using AutoElite.Mobile.Schedule;
using Microsoft.Kiota.Abstractions;

namespace AutoElite.Mobile.Pages;

/// <summary>
/// A lead for a manager: who the person is, how to reach them, what they wrote, where the lead is in the funnel and its
/// notes, read from the API or, without a network, from the device. The manager calls or writes to the person, takes a
/// new lead on, which makes them its manager when it has none and which their limit of leads in progress may not allow,
/// writes a note, marks an open lead lost with the reason and opens a lost one again; these need the network. The
/// contract of a lead is drawn up on the website.
/// </summary>
public sealed class LeadViewModel : ViewModel, IQueryAttributable
{
    /// <summary>The name of the parameter with the <see cref="LeadRequest"/>.</summary>
    public const string LeadParameter = "lead";

    private readonly LeadBook _leads;
    private Guid _leadId;
    private string? _phone;
    private string? _email;

    /// <summary>Initializes a new instance of the <see cref="LeadViewModel"/> class.</summary>
    /// <param name="leads">The leads of the school.</param>
    public LeadViewModel(LeadBook leads)
    {
        _leads = leads;
        CallCommand = new Command(async () => await CallAsync(), () => HasPhone);
        EmailCommand = new Command(async () => await EmailAsync(), () => HasEmail);
        TakeCommand = new Command(async () => await RunAsync(TakeAsync), () => !IsBusy);
        LoseCommand = new Command(async () => await RunAsync(LoseAsync), () => !IsBusy);
        ReopenCommand = new Command(async () => await RunAsync(ReopenAsync), () => !IsBusy);
        AddNoteCommand = new Command(async () => await RunAsync(AddNoteAsync), () => !IsBusy);
    }

    /// <summary>Gets the name the person gave.</summary>
    public string Name
    {
        get;
        private set => SetProperty(ref field, value);
    } = string.Empty;

    /// <summary>Gets the phone number and the email address of the person.</summary>
    public string Contacts
    {
        get;
        private set => SetProperty(ref field, value);
    } = string.Empty;

    /// <summary>Gets where the lead came from, the licence category wanted and when it arrived.</summary>
    public string Details
    {
        get;
        private set => SetProperty(ref field, value);
    } = string.Empty;

    /// <summary>Gets what the person wrote, if anything.</summary>
    public string? Comment
    {
        get;
        private set
        {
            if (SetProperty(ref field, value))
                OnPropertyChanged(nameof(HasComment));
        }
    }

    /// <summary>Gets a value indicating whether the person wrote something.</summary>
    public bool HasComment => !string.IsNullOrWhiteSpace(Comment);

    /// <summary>Gets the status of the lead, with the reason of a lost one, and its manager.</summary>
    public string State
    {
        get;
        private set => SetProperty(ref field, value);
    } = string.Empty;

    /// <summary>Gets the notes of the lead, the latest first.</summary>
    public IReadOnlyList<string> Notes
    {
        get;
        private set
        {
            if (SetProperty(ref field, value))
                OnPropertyChanged(nameof(HasNotes));
        }
    } = [];

    /// <summary>Gets a value indicating whether the lead has notes.</summary>
    public bool HasNotes => Notes.Count > 0;

    /// <summary>Gets or sets the note being written.</summary>
    public string NoteText
    {
        get;
        set => SetProperty(ref field, value);
    } = string.Empty;

    /// <summary>Gets a value indicating whether the person has a phone number to call.</summary>
    public bool HasPhone
    {
        get;
        private set
        {
            if (SetProperty(ref field, value))
                CallCommand.ChangeCanExecute();
        }
    }

    /// <summary>Gets a value indicating whether the person has an email address to write to.</summary>
    public bool HasEmail
    {
        get;
        private set
        {
            if (SetProperty(ref field, value))
                EmailCommand.ChangeCanExecute();
        }
    }

    /// <summary>Gets a value indicating whether the lead is new, so that it can be taken on.</summary>
    public bool IsNew
    {
        get;
        private set => SetProperty(ref field, value);
    }

    /// <summary>Gets a value indicating whether the lead is open, new or in progress, so that it can be marked lost.</summary>
    public bool IsOpen
    {
        get;
        private set => SetProperty(ref field, value);
    }

    /// <summary>Gets a value indicating whether the lead is lost, so that it can be opened again.</summary>
    public bool IsLost
    {
        get;
        private set => SetProperty(ref field, value);
    }

    /// <summary>Gets that the lead is from the device, or why it could not be read or an action failed.</summary>
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

    /// <summary>Gets a value indicating whether an action is being sent.</summary>
    public bool IsBusy
    {
        get;
        private set
        {
            if (!SetProperty(ref field, value))
                return;

            TakeCommand.ChangeCanExecute();
            LoseCommand.ChangeCanExecute();
            ReopenCommand.ChangeCanExecute();
            AddNoteCommand.ChangeCanExecute();
        }
    }

    /// <summary>Gets the command that calls the person.</summary>
    public Command CallCommand { get; }

    /// <summary>Gets the command that writes an email to the person.</summary>
    public Command EmailCommand { get; }

    /// <summary>Gets the command that takes a new lead on.</summary>
    public Command TakeCommand { get; }

    /// <summary>Gets the command that marks an open lead lost, asking for the reason.</summary>
    public Command LoseCommand { get; }

    /// <summary>Gets the command that opens a lost lead again.</summary>
    public Command ReopenCommand { get; }

    /// <summary>Gets the command that writes the note.</summary>
    public Command AddNoteCommand { get; }

    /// <inheritdoc />
    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        ArgumentNullException.ThrowIfNull(query);
        if (!query.TryGetValue(LeadParameter, out var value) || value is not LeadRequest request)
            return;

        _leadId = request.LeadId;
        Name = request.Name;
    }

    /// <summary>Reads the lead.</summary>
    /// <returns>A task that completes when the page shows it.</returns>
    public async Task LoadAsync()
    {
        try
        {
            var lead = await _leads.ReadAsync(_leadId, CancellationToken.None);
            Show(lead.Value);
            Notice = lead.IsFromCache
                ? AppTexts.Format("Leads.Offline", lead.SavedAt.ToLocalTime().ToString("g", CultureInfo.CurrentCulture))
                : null;
        }
        catch (Exception exception) when (exception is ApiException or HttpRequestException)
        {
            Notice = ProblemTexts.Of(exception);
        }
    }

    private static string NoteOf(LeadNoteItem note) =>
        LessonTexts.DetailsOf(
            note.WrittenAtUtc?.ToLocalTime().ToString("g", CultureInfo.CurrentCulture), note.AuthorName, note.Text);

    private void Show(LeadDetails lead)
    {
        Name = lead.ContactName ?? string.Empty;
        _phone = string.IsNullOrWhiteSpace(lead.Phone) ? null : lead.Phone;
        _email = string.IsNullOrWhiteSpace(lead.Email) ? null : lead.Email;
        HasPhone = _phone is not null;
        HasEmail = _email is not null;
        Contacts = LessonTexts.DetailsOf(lead.Phone, lead.Email);
        Details = LeadTexts.DetailsOf(lead.Source, lead.DesiredCategory, lead.CreatedAtUtc);
        Comment = lead.Comment;
        State = LessonTexts.DetailsOf(
            LeadTexts.StatusOf(lead.Status),
            lead.Status == LeadStatus.Lost ? lead.LostReason : null,
            lead.ManagerName is { } manager ? AppTexts.Format("Lead.Manager", manager) : AppTexts.Get("Lead.NoManager"));
        Notes = [.. (lead.Notes ?? []).OfType<LeadNoteItem>().OrderByDescending(note => note.WrittenAtUtc).Select(NoteOf)];
        IsNew = lead.Status == LeadStatus.New;
        IsOpen = lead.Status is LeadStatus.New or LeadStatus.InProgress;
        IsLost = lead.Status == LeadStatus.Lost;
    }

    // Sends an action, then reads the lead again; a failure is shown on the page.
    private async Task RunAsync(Func<Task<bool>> action)
    {
        if (IsBusy)
            return;

        IsBusy = true;
        try
        {
            if (await action())
                await LoadAsync();
        }
        catch (Exception exception) when (exception is ApiException or HttpRequestException or TaskCanceledException)
        {
            Notice = ProblemTexts.Of(exception);
        }
        finally
        {
            IsBusy = false;
        }
    }

    // The API makes a manager who takes on a lead without a manager its manager, within their limit of leads in progress.
    private async Task<bool> TakeAsync()
    {
        await _leads.StartAsync(_leadId, CancellationToken.None);

        return true;
    }

    private async Task<bool> LoseAsync()
    {
        var reason = await Shell.Current.DisplayPromptAsync(
            AppTexts.Get("Lead.LostTitle"),
            AppTexts.Get("Lead.LostReason"),
            AppTexts.Get("Lead.MarkLost"),
            AppTexts.Get("Common.Cancel"),
            maxLength: 500);
        if (string.IsNullOrWhiteSpace(reason))
            return false;

        await _leads.MarkLostAsync(_leadId, reason.Trim(), CancellationToken.None);

        return true;
    }

    private async Task<bool> ReopenAsync()
    {
        await _leads.ReopenAsync(_leadId, CancellationToken.None);

        return true;
    }

    private async Task<bool> AddNoteAsync()
    {
        var text = NoteText.Trim();
        if (text.Length == 0)
            return false;

        await _leads.AddNoteAsync(_leadId, text, CancellationToken.None);
        NoteText = string.Empty;

        return true;
    }

    private async Task CallAsync()
    {
        if (_phone is { } phone && !await PhoneCalls.CallAsync(phone))
            Notice = AppTexts.Format("Student.PhoneCopied", phone);
    }

    private async Task EmailAsync()
    {
        if (_email is { } email && !await Launcher.Default.TryOpenAsync(new Uri("mailto:" + email)))
        {
            await Clipboard.Default.SetTextAsync(email);
            Notice = AppTexts.Format("Lead.EmailCopied", email);
        }
    }
}
