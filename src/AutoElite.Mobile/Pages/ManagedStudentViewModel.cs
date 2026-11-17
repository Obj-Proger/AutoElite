using System.Globalization;
using AutoElite.Client.Api.Models;
using AutoElite.Mobile.Localization;
using AutoElite.Mobile.Schedule;
using Microsoft.Kiota.Abstractions;

namespace AutoElite.Mobile.Pages;

/// <summary>
/// The card of a student for a manager or an administrator: how to reach them, then each contract with what was paid
/// and what is owed, the progress of the training and the payments, read from the API or, without a network, from the
/// device. The manager writes to the student, calls them, records a payment towards a contract and confirms a payment
/// whose money has come; these need the network.
/// </summary>
public sealed class ManagedStudentViewModel : ViewModel, IQueryAttributable
{
    /// <summary>The name of the parameter with the <see cref="StudentRequest"/>.</summary>
    public const string StudentParameter = "student";

    private readonly StudentDirectory _directory;
    private readonly Chats _chats;
    private Guid _studentId;

    /// <summary>Initializes a new instance of the <see cref="ManagedStudentViewModel"/> class.</summary>
    /// <param name="directory">The students of the school.</param>
    /// <param name="chats">The chats, to write to the student.</param>
    public ManagedStudentViewModel(StudentDirectory directory, Chats chats)
    {
        _directory = directory;
        _chats = chats;
        WriteCommand = new Command(async () => await WriteAsync());
        CallCommand = new Command(async () => await CallAsync(), () => HasPhone);
        RefreshCommand = new Command(async () => await RefreshAsync());
    }

    /// <summary>Gets the full name of the student.</summary>
    public string Name
    {
        get;
        private set
        {
            if (SetProperty(ref field, value))
                OnPropertyChanged(nameof(Initials));
        }
    } = string.Empty;

    /// <summary>Gets the initials of the student, shown in place of a photo.</summary>
    public string Initials => PhoneCalls.InitialsOf(Name);

    /// <summary>Gets the phone number and the email address of the student.</summary>
    public string Contacts
    {
        get;
        private set => SetProperty(ref field, value);
    } = string.Empty;

    /// <summary>Gets a value indicating whether the student has a phone number to call.</summary>
    public bool HasPhone
    {
        get;
        private set
        {
            if (SetProperty(ref field, value))
                CallCommand.ChangeCanExecute();
        }
    }

    /// <summary>Gets the contracts of the student, the newest first.</summary>
    public IReadOnlyList<ManagedContractEntry> ContractEntries
    {
        get;
        private set => SetProperty(ref field, value);
    } = [];

    /// <summary>Gets that the card is from the device, or why it could not be read or an action failed.</summary>
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

    /// <summary>Gets or sets a value indicating whether the page is pulled down to read the card again.</summary>
    public bool IsRefreshing
    {
        get;
        set => SetProperty(ref field, value);
    }

    /// <summary>Gets the command that opens the personal chat with the student.</summary>
    public Command WriteCommand { get; }

    /// <summary>Gets the command that calls the student.</summary>
    public Command CallCommand { get; }

    /// <summary>Gets the command that reads the card again.</summary>
    public Command RefreshCommand { get; }

    private string? Phone { get; set; }

    /// <inheritdoc />
    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        ArgumentNullException.ThrowIfNull(query);
        if (!query.TryGetValue(StudentParameter, out var value) || value is not StudentRequest request)
            return;

        _studentId = request.StudentId;
        Name = request.Name;
    }

    /// <summary>Reads the card of the student and the payments of their contracts.</summary>
    /// <returns>A task that completes when the page shows them.</returns>
    public async Task LoadAsync()
    {
        try
        {
            var card = await _directory.ReadCardAsync(_studentId, CancellationToken.None);
            var student = card.Value;
            Name = string.Join(
                ' ', new[] { student.LastName, student.FirstName, student.MiddleName }.Where(part => !string.IsNullOrWhiteSpace(part)));
            Phone = string.IsNullOrWhiteSpace(student.Phone) ? null : student.Phone;
            HasPhone = Phone is not null;
            Contacts = LessonTexts.DetailsOf(student.Phone, student.Email);

            var entries = new List<ManagedContractEntry>();
            var savedAt = card.IsFromCache ? card.SavedAt : (DateTimeOffset?)null;
            foreach (var item in (student.Contracts ?? []).OfType<StudentCardContract>())
            {
                if (item.Contract is not { Id: { } contractId } contract)
                    continue;

                var payments = await _directory.ReadPaymentsAsync(contractId, CancellationToken.None);
                if (payments.IsFromCache && (savedAt is null || payments.SavedAt < savedAt))
                    savedAt = payments.SavedAt;

                entries.Add(Entry(contract, item.Group, payments.Value));
            }

            ContractEntries = entries;
            Notice = savedAt is { } saved
                ? AppTexts.Format("Contracts.Offline", saved.ToLocalTime().ToString("g", CultureInfo.CurrentCulture))
                : entries.Count == 0 ? AppTexts.Get("Contracts.Empty") : null;
        }
        catch (Exception exception) when (exception is ApiException or HttpRequestException)
        {
            Notice = ProblemTexts.Of(exception);
        }
    }

    private static string GearboxOf(Transmission? transmission) =>
        transmission == Transmission.Automatic ? AppTexts.Get("OfferSlot.Automatic") : AppTexts.Get("OfferSlot.Manual");

    private static string Dates(ContractDetails contract)
    {
        var culture = CultureInfo.CurrentCulture;

        return contract is { StartDate: { } start, EndDate: { } end }
            ? string.Create(culture, $"{start.DateTime.ToString("d", culture)} – {end.DateTime.ToString("d", culture)}")
            : string.Empty;
    }

    private ManagedContractEntry Entry(ContractDetails contract, StudentCardGroup? group, IEnumerable<PaymentItem> payments)
    {
        var culture = CultureInfo.CurrentCulture;
        var completed = contract.CompletedPracticeHours ?? 0;
        var course = contract.CoursePracticeHours ?? 0;
        var debt = contract.DebtAmount ?? 0;
        var state = contract.Status switch
        {
            ContractStatus.Active => AppTexts.Get("Contracts.Active"),
            ContractStatus.Completed => AppTexts.Get("Contracts.Completed"),
            _ => AppTexts.Get("Contracts.Terminated")
        };
        var title = LessonTexts.DetailsOf(contract.Number, contract.CourseName);
        var request = new PaymentRequest(contract.Id!.Value, title, contract.Currency, Math.Max(debt, 0));

        return new ManagedContractEntry
        {
            Course = LessonTexts.DetailsOf(contract.CourseName, GearboxOf(contract.Transmission)),
            Number = LessonTexts.DetailsOf(contract.Number, state, Dates(contract)),
            Money = debt > 0
                ? AppTexts.Format(
                    "Contracts.MoneyOwed",
                    ContractsViewModel.MoneyOf(contract.PaidAmount, contract.Currency),
                    ContractsViewModel.MoneyOf(contract.PriceAmount, contract.Currency),
                    ContractsViewModel.MoneyOf(debt, contract.Currency))
                : AppTexts.Format(
                    "Contracts.MoneyPaid",
                    ContractsViewModel.MoneyOf(contract.PaidAmount, contract.Currency),
                    ContractsViewModel.MoneyOf(contract.PriceAmount, contract.Currency)),
            HasDebt = debt > 0,
            Practice = AppTexts.Format("Contracts.Practice", completed.ToString("0.##", culture), course),
            PracticeProgress = course > 0 ? Math.Clamp(completed / course, 0d, 1d) : 0,
            Theory = group is { GroupName: { } groupName }
                ? AppTexts.Format("Student.Group", groupName, group.LessonsAttended ?? 0, group.LessonsHeld ?? 0)
                : AppTexts.Format("Contracts.Theory", contract.AttendedTheoryLessons ?? 0),
            Payments = [.. payments.Select(PaymentOf)],
            AddPaymentCommand = contract.Status == ContractStatus.Active
                ? new Command(async () => await Routes.OpenAsync(
                    Routes.Payment, new Dictionary<string, object> { [PaymentViewModel.ContractParameter] = request }))
                : null
        };
    }

    private PaymentEntry PaymentOf(PaymentItem payment) => new()
    {
        Text = ContractsViewModel.Payment(payment),
        ConfirmCommand = payment is { Status: PaymentStatus.Registered, Id: { } paymentId }
            ? new Command(async () => await ConfirmAsync(paymentId, ContractsViewModel.MoneyOf(payment.Amount, payment.Currency)))
            : null
    };

    // The manager confirms that the money of the payment has come; the payment then counts towards the debt.
    private async Task ConfirmAsync(Guid paymentId, string amount)
    {
        var isConfirmed = await Shell.Current.DisplayAlertAsync(
            AppTexts.Get("Payment.ConfirmTitle"),
            AppTexts.Format("Payment.ConfirmText", amount),
            AppTexts.Get("Payment.Confirm"),
            AppTexts.Get("Common.Cancel"));
        if (!isConfirmed)
            return;

        try
        {
            await _directory.ConfirmPaymentAsync(paymentId, CancellationToken.None);
            await LoadAsync();
        }
        catch (Exception exception) when (exception is ApiException or HttpRequestException or TaskCanceledException)
        {
            Notice = ProblemTexts.Of(exception);
        }
    }

    private async Task CallAsync()
    {
        if (Phone is { } phone && !await PhoneCalls.CallAsync(phone))
            Notice = AppTexts.Format("Student.PhoneCopied", phone);
    }

    private async Task WriteAsync()
    {
        try
        {
            var chatId = await _chats.OpenDirectAsync(_studentId, CancellationToken.None);
            await Routes.OpenAsync(
                Routes.Chat,
                new Dictionary<string, object> { [ChatViewModel.ChatParameter] = new ChatRequest(chatId, Name, IsPersonal: true) });
        }
        catch (Exception exception) when (exception is ApiException or HttpRequestException or TaskCanceledException
            or InvalidOperationException)
        {
            Notice = ProblemTexts.Of(exception);
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
