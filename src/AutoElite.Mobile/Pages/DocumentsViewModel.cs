using System.Globalization;
using AutoElite.Client.Api.Models;
using AutoElite.Mobile.Localization;
using AutoElite.Mobile.Schedule;
using AutoElite.Mobile.Session;
using Microsoft.Kiota.Abstractions;

namespace AutoElite.Mobile.Pages;

/// <summary>
/// The documents of a student: their own, which they add from the device with a description and delete, and those the
/// school attached to each of their contracts, which they open. From the API or, without a network, from the device.
/// </summary>
public sealed class DocumentsViewModel : ViewModel
{
    private readonly StudentDocuments _documents;
    private readonly DocumentFiles _files;
    private readonly UserSession _session;
    private readonly StudentContracts _contracts;
    private Guid? _userId;

    /// <summary>Initializes a new instance of the <see cref="DocumentsViewModel"/> class.</summary>
    /// <param name="documents">The documents of the student.</param>
    /// <param name="files">Opens the files.</param>
    /// <param name="session">The user.</param>
    /// <param name="contracts">The contracts of the student.</param>
    public DocumentsViewModel(StudentDocuments documents, DocumentFiles files, UserSession session, StudentContracts contracts)
    {
        _documents = documents;
        _files = files;
        _session = session;
        _contracts = contracts;
        AddCommand = new Command(async () => await AddAsync(), () => !IsBusy);
    }

    /// <summary>Gets the documents of the user.</summary>
    public IReadOnlyList<DocumentEntry> Mine
    {
        get;
        private set
        {
            if (SetProperty(ref field, value))
                OnPropertyChanged(nameof(HasNoneOfMine));
        }
    } = [];

    /// <summary>Gets a value indicating whether the user has added no documents.</summary>
    public bool HasNoneOfMine => Mine.Count == 0;

    /// <summary>Gets the documents of the contracts that have some.</summary>
    public IReadOnlyList<DocumentGroup> Contracts
    {
        get;
        private set
        {
            if (SetProperty(ref field, value))
                OnPropertyChanged(nameof(HasContracts));
        }
    } = [];

    /// <summary>Gets a value indicating whether a contract has documents.</summary>
    public bool HasContracts => Contracts.Count > 0;

    /// <summary>Gets that the documents are from the device, or why something failed, or <see langword="null"/>.</summary>
    public string? Notice
    {
        get;
        private set
        {
            if (SetProperty(ref field, value))
                OnPropertyChanged(nameof(HasNotice));
        }
    }

    /// <summary>Gets a value indicating whether there is a notice.</summary>
    public bool HasNotice => Notice is not null;

    /// <summary>Gets a value indicating whether a document is being added.</summary>
    public bool IsBusy
    {
        get;
        private set
        {
            if (SetProperty(ref field, value))
                AddCommand.ChangeCanExecute();
        }
    }

    /// <summary>Gets the command that adds a document from the device.</summary>
    public Command AddCommand { get; }

    /// <summary>Reads the documents, each time the page appears.</summary>
    /// <returns>A task that completes when the page shows them.</returns>
    public async Task LoadAsync()
    {
        try
        {
            _userId = (await _session.GetProfileAsync(CancellationToken.None)).Value.Id;
            if (_userId is not { } userId)
                return;

            var mine = await _documents.ReadAsync(EntityKind.User, userId, CancellationToken.None);
            Mine = [.. mine.Value.Where(document => document.Id is not null).Select(document => Entry(document, userId))];
            var offlineSince = mine.IsFromCache ? mine.SavedAt : (DateTimeOffset?)null;

            var groups = new List<DocumentGroup>();
            var contracts = await _contracts.ReadContractsAsync(CancellationToken.None);
            foreach (var contract in contracts.Value.Where(contract => contract.Id is not null))
            {
                var documents = await _documents.ReadAsync(EntityKind.Contract, contract.Id!.Value, CancellationToken.None);
                if (documents.IsFromCache && (offlineSince is null || documents.SavedAt < offlineSince))
                    offlineSince = documents.SavedAt;

                List<DocumentEntry> entries =
                    [.. documents.Value.Where(document => document.Id is not null).Select(document => Entry(document, userId))];
                if (entries.Count > 0)
                {
                    groups.Add(new DocumentGroup
                    {
                        Title = LessonTexts.DetailsOf(contract.Number, contract.CourseName),
                        Documents = entries
                    });
                }
            }

            Contracts = groups;
            Notice = offlineSince is { } savedAt
                ? AppTexts.Format("Documents.Offline", savedAt.ToLocalTime().ToString("g", CultureInfo.CurrentCulture))
                : null;
        }
        catch (Exception exception) when (exception is ApiException or HttpRequestException)
        {
            Notice = ProblemTexts.Of(exception);
        }
    }

    private DocumentEntry Entry(DocumentItem document, Guid userId)
    {
        var documentId = document.Id!.Value;
        var name = document.FileName ?? AppTexts.Get("Chat.File");

        return new DocumentEntry
        {
            Name = name,
            Details = LessonTexts.DetailsOf(
                document.Description,
                document.SizeBytes is { } size ? FileSizes.Of(size) : null,
                document.UploadedByUserId == userId ? null : document.UploadedByName,
                document.UploadedAtUtc?.ToLocalTime().ToString("d", CultureInfo.CurrentCulture)),
            OpenCommand = new Command(async () => await OpenAsync(documentId, name)),
            DeleteCommand = document.UploadedByUserId == userId ? new Command(async () => await DeleteAsync(documentId, name)) : null
        };
    }

    private async Task OpenAsync(Guid documentId, string name)
    {
        try
        {
            await _files.OpenAsync(documentId, name, CancellationToken.None);
        }
        catch (Exception exception) when (exception is ApiException or HttpRequestException or TaskCanceledException
            or InvalidOperationException or IOException)
        {
            Notice = ProblemTexts.Of(exception);
        }
    }

    private async Task AddAsync()
    {
        if (_userId is not { } userId)
            return;

        FileResult? file;
        try
        {
            file = await FilePicker.Default.PickAsync(new PickOptions { PickerTitle = AppTexts.Get("Documents.Choose") });
        }
        catch (PermissionException)
        {
            Notice = AppTexts.Get("Chat.NoFileAccess");

            return;
        }

        if (file is null)
            return;

        var description = await Shell.Current.DisplayPromptAsync(
            AppTexts.Get("Documents.DescriptionTitle"),
            AppTexts.Get("Documents.DescriptionQuestion"),
            AppTexts.Get("Documents.Add"),
            AppTexts.Get("Common.Cancel"),
            maxLength: 500);
        if (description is null)
            return;

        IsBusy = true;
        try
        {
            await _documents.AddAsync(userId, file, description, CancellationToken.None);
            await LoadAsync();
        }
        catch (Exception exception) when (exception is ApiException or HttpRequestException or TaskCanceledException
            or IOException)
        {
            Notice = ProblemTexts.Of(exception);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task DeleteAsync(Guid documentId, string name)
    {
        if (!await Shell.Current.DisplayAlertAsync(
                AppTexts.Get("Documents.DeleteTitle"), name, AppTexts.Get("Documents.Delete"), AppTexts.Get("Common.Cancel")))
        {
            return;
        }

        try
        {
            await _documents.DeleteAsync(documentId, CancellationToken.None);
            await LoadAsync();
        }
        catch (Exception exception) when (exception is ApiException or HttpRequestException or TaskCanceledException)
        {
            Notice = ProblemTexts.Of(exception);
        }
    }
}
