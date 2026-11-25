using System.Globalization;
using AutoElite.Client.Api.Models;
using AutoElite.Client.Offline;
using AutoElite.Mobile.Controls;
using AutoElite.Mobile.Localization;
using AutoElite.Mobile.Schedule;
using Microsoft.Kiota.Abstractions;

namespace AutoElite.Mobile.Pages;

/// <summary>
/// The money the office follows, for a manager or an administrator, in two views: the payments recorded but not
/// confirmed, the latest first, each confirmed once its money has come or cancelled when it was recorded by mistake;
/// and the students who owe money on an active contract, the largest debt first, each opening the card of the student,
/// where the manager calls them or records a payment. Each view says how much it comes to, per currency. The lists are
/// read from the API or, without a network, from the device; the actions need the network.
/// </summary>
public sealed class MoneyViewModel : ViewModel, IQueryAttributable
{
    /// <summary>The name of the parameter that chooses the view: <see cref="DebtsView"/> for the debts.</summary>
    public const string ViewParameter = "view";

    /// <summary>The value of <see cref="ViewParameter"/> that opens the debts.</summary>
    public const string DebtsView = "debts";

    private readonly OfficeMoney _money;

    // Whether the lists have been read once, so that the empty view is not announced before.
    private bool _isLoaded;

    /// <summary>Initializes a new instance of the <see cref="MoneyViewModel"/> class.</summary>
    /// <param name="money">The money of the school.</param>
    public MoneyViewModel(OfficeMoney money)
    {
        _money = money;
        ShowWaitingCommand = new Command(() => ShowsDebts = false);
        ShowDebtsCommand = new Command(() => ShowsDebts = true);
        RefreshCommand = new Command(async () => await RefreshAsync());
    }

    /// <summary>Gets a value indicating whether the debts are shown instead of the waiting payments.</summary>
    public bool ShowsDebts
    {
        get;
        private set
        {
            if (SetProperty(ref field, value))
                OnViewChanged();
        }
    }

    /// <summary>Gets a value indicating whether the waiting payments are shown.</summary>
    public bool ShowsWaiting => !ShowsDebts;

    /// <summary>Gets the name of the view of the waiting payments, with their number.</summary>
    public string WaitingTitle => AppTexts.Format("Money.WaitingCount", Waiting.Count);

    /// <summary>Gets the name of the view of the debts, with their number.</summary>
    public string DebtsTitle => AppTexts.Format("Money.DebtsCount", Debts.Count);

    /// <summary>Gets the payments that wait for their confirmation.</summary>
    public IReadOnlyList<MoneyEntry> Waiting
    {
        get;
        private set
        {
            if (SetProperty(ref field, value))
                OnViewChanged();
        }
    } = [];

    /// <summary>Gets the contracts whose students owe money.</summary>
    public IReadOnlyList<MoneyEntry> Debts
    {
        get;
        private set
        {
            if (SetProperty(ref field, value))
                OnViewChanged();
        }
    } = [];

    /// <summary>Gets the rows of the view shown.</summary>
    public IReadOnlyList<MoneyEntry> Items => ShowsDebts ? Debts : Waiting;

    /// <summary>Gets how much the waiting payments come to, per currency.</summary>
    public string WaitingTotal
    {
        get;
        private set
        {
            if (SetProperty(ref field, value))
                OnPropertyChanged(nameof(Total));
        }
    } = string.Empty;

    /// <summary>Gets how much the students owe, per currency.</summary>
    public string DebtsTotal
    {
        get;
        private set
        {
            if (SetProperty(ref field, value))
                OnPropertyChanged(nameof(Total));
        }
    } = string.Empty;

    /// <summary>Gets how much the view shown comes to, or <see langword="null"/> when it is empty.</summary>
    public string? Total => Items.Count == 0 ? null : ShowsDebts ? DebtsTotal : WaitingTotal;

    /// <summary>Gets a value indicating whether the view shown has rows.</summary>
    public bool HasItems => Items.Count > 0;

    /// <summary>Gets what the view says when it has no rows.</summary>
    public string? EmptyText => Items.Count > 0 || !_isLoaded
        ? null
        : ShowsDebts ? AppTexts.Get("Money.NoDebts") : AppTexts.Get("Money.NoWaiting");

    /// <summary>Gets that the lists are from the device, or why they could not be read or an action failed.</summary>
    public string? Notice
    {
        get;
        private set => SetProperty(ref field, value);
    }

    /// <summary>Gets or sets a value indicating whether the page is pulled down to read the lists again.</summary>
    public bool IsRefreshing
    {
        get;
        set => SetProperty(ref field, value);
    }

    /// <summary>Gets the command that shows the waiting payments.</summary>
    public Command ShowWaitingCommand { get; }

    /// <summary>Gets the command that shows the debts.</summary>
    public Command ShowDebtsCommand { get; }

    /// <summary>Gets the command that reads the lists again.</summary>
    public Command RefreshCommand { get; }

    /// <inheritdoc />
    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        ArgumentNullException.ThrowIfNull(query);
        ShowsDebts = query.TryGetValue(ViewParameter, out var view) && view as string == DebtsView;
    }

    /// <summary>Reads both lists.</summary>
    /// <returns>A task that completes when the page shows them.</returns>
    public async Task LoadAsync()
    {
        try
        {
            var waiting = await _money.ReadWaitingAsync(CancellationToken.None);
            var debts = await _money.ReadDebtsAsync(CancellationToken.None);
            var payments = waiting.Value.Where(payment => payment.Id is not null).ToList();
            var contracts = debts.Value.Where(contract => contract.StudentId is not null && contract.DebtAmount > 0)
                .OrderByDescending(contract => contract.DebtAmount)
                .ToList();
            _isLoaded = true;
            Waiting = [.. payments.Select(WaitingEntry)];
            WaitingTotal = TotalOf(payments.Select(payment => (payment.Currency, payment.Amount ?? 0)));
            Debts = [.. contracts.Select(DebtEntry)];
            DebtsTotal = TotalOf(contracts.Select(contract => (contract.Currency, contract.DebtAmount ?? 0)));
            Notice = OfflineNotice(waiting, debts);
        }
        catch (Exception exception) when (exception is ApiException or HttpRequestException)
        {
            Notice = ProblemTexts.Of(exception);
        }
    }

    private static string? OfflineNotice<TFirst, TSecond>(Cached<TFirst> first, Cached<TSecond> second)
    {
        var savedAt = (first.IsFromCache, second.IsFromCache) switch
        {
            (true, true) => first.SavedAt < second.SavedAt ? first.SavedAt : second.SavedAt,
            (true, false) => first.SavedAt,
            (false, true) => second.SavedAt,
            _ => (DateTimeOffset?)null
        };

        return savedAt is { } saved
            ? AppTexts.Format("Money.Offline", saved.ToLocalTime().ToString("g", CultureInfo.CurrentCulture))
            : null;
    }

    /// <summary>What amounts come to; amounts in different currencies are never added up, each has its own sum.</summary>
    /// <param name="amounts">The amounts with their currencies.</param>
    /// <returns>The sums, as a line.</returns>
    internal static string TotalOf(IEnumerable<(string? Currency, double Amount)> amounts) =>
        AppTexts.Format(
            "Money.Total",
            string.Join(
                ", ",
                amounts.GroupBy(amount => amount.Currency ?? string.Empty, StringComparer.Ordinal)
                    .OrderBy(group => group.Key, StringComparer.Ordinal)
                    .Select(group => ContractsViewModel.MoneyOf(group.Sum(amount => amount.Amount), group.Key))));

    private static string MethodOf(PaymentMethod? method) => method switch
    {
        PaymentMethod.Cash => AppTexts.Get("Payment.Cash"),
        PaymentMethod.BankCard => AppTexts.Get("Payment.BankCard"),
        PaymentMethod.BankTransfer => AppTexts.Get("Payment.BankTransfer"),
        _ => AppTexts.Get("Money.Online")
    };

    private static Task OpenStudentAsync(Guid studentId, string name) =>
        Routes.OpenAsync(
            Routes.ManagedStudent,
            new Dictionary<string, object> { [ManagedStudentViewModel.StudentParameter] = new StudentRequest(studentId, name) });

    private MoneyEntry WaitingEntry(PaymentItem payment)
    {
        var name = payment.StudentName ?? string.Empty;
        var amount = ContractsViewModel.MoneyOf(payment.Amount, payment.Currency);

        return new MoneyEntry
        {
            Title = name,
            Subtitle = LessonTexts.DetailsOf(
                payment.ContractNumber,
                MethodOf(payment.Method),
                payment.RegisteredAtUtc?.ToLocalTime().ToString("d MMM", CultureInfo.CurrentCulture),
                payment.RegisteredByName),
            Amount = amount,
            Detail = payment.Purpose,
            Tone = StatusTone.Warning,
            Command = new Command(async () => await ChooseAsync(payment.Id!.Value, payment.StudentId, name, amount))
        };
    }

    private static MoneyEntry DebtEntry(ContractListItem contract)
    {
        var name = contract.StudentName ?? string.Empty;
        var studentId = contract.StudentId!.Value;

        return new MoneyEntry
        {
            Title = name,
            Subtitle = LessonTexts.DetailsOf(contract.Number, contract.CourseName),
            Amount = ContractsViewModel.MoneyOf(contract.DebtAmount, contract.Currency),
            Detail = AppTexts.Format("Money.OfPrice", ContractsViewModel.MoneyOf(contract.PriceAmount, contract.Currency)),
            Tone = StatusTone.Danger,
            Command = new Command(async () => await OpenStudentAsync(studentId, name))
        };
    }

    // What the manager does with a waiting payment: confirm it, cancel it, or look at the student first.
    private async Task ChooseAsync(Guid paymentId, Guid? studentId, string name, string amount)
    {
        var confirm = AppTexts.Get("Payment.Confirm");
        var student = AppTexts.Get("Money.OpenStudent");
        var cancel = AppTexts.Get("Money.CancelPayment");
        string[] choices = studentId is null ? [confirm] : [confirm, student];
        var choice = await Shell.Current.DisplayActionSheetAsync(
            AppTexts.Format("Money.PaymentOf", amount, name), AppTexts.Get("Common.Cancel"), cancel, choices);
        if (choice == confirm)
            await ConfirmAsync(paymentId, amount);
        else if (choice == cancel)
            await CancelAsync(paymentId, amount);
        else if (choice == student && studentId is { } id)
            await OpenStudentAsync(id, name);
    }

    private async Task ConfirmAsync(Guid paymentId, string amount)
    {
        var isConfirmed = await Shell.Current.DisplayAlertAsync(
            AppTexts.Get("Payment.ConfirmTitle"),
            AppTexts.Format("Payment.ConfirmText", amount),
            AppTexts.Get("Payment.Confirm"),
            AppTexts.Get("Common.Cancel"));
        if (isConfirmed)
            await ActAsync(token => _money.ConfirmAsync(paymentId, token));
    }

    // A payment recorded by mistake or whose money never came; the reason stays in its history.
    private async Task CancelAsync(Guid paymentId, string amount)
    {
        var reason = await Shell.Current.DisplayPromptAsync(
            AppTexts.Get("Money.CancelTitle"),
            AppTexts.Format("Money.CancelReason", amount),
            AppTexts.Get("Money.CancelPayment"),
            AppTexts.Get("Common.Cancel"),
            maxLength: 1000);
        if (!string.IsNullOrWhiteSpace(reason))
            await ActAsync(token => _money.CancelAsync(paymentId, reason.Trim(), token));
    }

    private async Task ActAsync(Func<CancellationToken, Task> action)
    {
        try
        {
            await action(CancellationToken.None);
            await LoadAsync();
        }
        catch (Exception exception) when (exception is ApiException or HttpRequestException or TaskCanceledException)
        {
            Notice = ProblemTexts.Of(exception);
        }
    }

    private void OnViewChanged()
    {
        OnPropertyChanged(nameof(ShowsWaiting));
        OnPropertyChanged(nameof(Items));
        OnPropertyChanged(nameof(HasItems));
        OnPropertyChanged(nameof(EmptyText));
        OnPropertyChanged(nameof(Total));
        OnPropertyChanged(nameof(WaitingTitle));
        OnPropertyChanged(nameof(DebtsTitle));
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
