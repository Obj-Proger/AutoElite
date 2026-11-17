using AutoElite.Client.Api.Models;
using AutoElite.Mobile.Localization;
using AutoElite.Mobile.Schedule;
using Microsoft.Kiota.Abstractions;

namespace AutoElite.Mobile.Pages;

/// <summary>
/// The booking of a student on a free practice slot by a manager: the student is found by a part of the name, the email
/// address or the phone number; their active contract for the category and the gearbox of the vehicle is taken, or
/// chosen when they have several, and after a confirmation the slot is booked and the page closes.
/// </summary>
public sealed class BookStudentViewModel : ViewModel, IQueryAttributable
{
    /// <summary>The name of the parameter with the <see cref="BookStudentRequest"/>.</summary>
    public const string SlotParameter = "slot";

    private readonly StudentDirectory _directory;
    private BookStudentRequest? _request;

    /// <summary>Initializes a new instance of the <see cref="BookStudentViewModel"/> class.</summary>
    /// <param name="directory">The students of the school.</param>
    /// <param name="search">The search of the students.</param>
    public BookStudentViewModel(StudentDirectory directory, StudentSearch search)
    {
        ArgumentNullException.ThrowIfNull(search);
        _directory = directory;
        Search = search;
        Search.Chosen = BookAsync;
    }

    /// <summary>Gets the search of the students.</summary>
    public StudentSearch Search { get; }

    /// <summary>Gets when, with whom and on what the lesson is.</summary>
    public string Summary
    {
        get;
        private set => SetProperty(ref field, value);
    } = string.Empty;

    /// <summary>Gets why the student cannot be booked, or why booking failed.</summary>
    public string? Message
    {
        get;
        private set
        {
            if (SetProperty(ref field, value))
                OnPropertyChanged(nameof(HasMessage));
        }
    }

    /// <summary>Gets a value indicating whether there is a message to show.</summary>
    public bool HasMessage => Message is not null;

    /// <summary>Gets a value indicating whether the student is being booked.</summary>
    public bool IsBusy
    {
        get;
        private set => SetProperty(ref field, value);
    }

    /// <inheritdoc />
    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        ArgumentNullException.ThrowIfNull(query);
        if (!query.TryGetValue(SlotParameter, out var value) || value is not BookStudentRequest request)
            return;

        _request = request;
        Summary = request.Summary;
        Message = null;
    }

    /// <summary>Shows the first students, before anything is typed.</summary>
    /// <returns>A task that completes when they are shown.</returns>
    public Task LoadAsync() => Search.SearchNowAsync();

    // The contract a booking is for: the only one that fits, or the one the manager chooses.
    private static async Task<ContractDetails?> ChooseAsync(List<ContractDetails> contracts)
    {
        if (contracts.Count == 1)
            return contracts[0];

        string[] labels = [.. contracts.Select(contract => LessonTexts.DetailsOf(contract.Number, contract.CourseName))];
        var choice = await Shell.Current.DisplayActionSheetAsync(
            AppTexts.Get("BookStudent.ChooseContract"), AppTexts.Get("Common.Cancel"), null, labels);
        var index = Array.IndexOf(labels, choice);

        return index >= 0 ? contracts[index] : null;
    }

    private async Task BookAsync(Guid studentId, string name)
    {
        if (IsBusy || _request is not { } request)
            return;

        IsBusy = true;
        Message = null;
        try
        {
            var card = (await _directory.ReadCardAsync(studentId, CancellationToken.None)).Value;
            List<ContractDetails> fitting =
            [
                .. (card.Contracts ?? [])
                    .Select(item => item?.Contract)
                    .OfType<ContractDetails>()
                    .Where(contract => contract.Id is not null
                        && contract.Status == ContractStatus.Active
                        && (request.Category is null || contract.Category == request.Category)
                        && (request.Transmission is null || contract.Transmission == request.Transmission))
            ];
            if (fitting.Count == 0)
            {
                Message = AppTexts.Format("BookStudent.NoContract", name);

                return;
            }

            if (await ChooseAsync(fitting) is not { Id: { } contractId })
                return;

            var isConfirmed = await Shell.Current.DisplayAlertAsync(
                AppTexts.Get("BookStudent.ConfirmTitle"),
                AppTexts.Format("BookStudent.Confirm", name, request.Summary),
                AppTexts.Get("BookStudent.Book"),
                AppTexts.Get("Common.Cancel"));
            if (!isConfirmed)
                return;

            await _directory.BookAsync(request.SlotId, contractId, CancellationToken.None);
            await Shell.Current.GoToAsync("..");
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
}
