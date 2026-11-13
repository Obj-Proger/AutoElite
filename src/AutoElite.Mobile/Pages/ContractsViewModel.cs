using System.Globalization;
using AutoElite.Client.Api.Models;
using AutoElite.Mobile.Localization;
using AutoElite.Mobile.Schedule;
using Microsoft.Kiota.Abstractions;

namespace AutoElite.Mobile.Pages;

/// <summary>
/// The contracts of a student, the active ones first: the progress of the training in driving hours and theory
/// lessons, the quality score, the money paid and owed, and the payments, from the API or, without a network, from the
/// device.
/// </summary>
public sealed class ContractsViewModel : ViewModel
{
    private readonly StudentContracts _contracts;

    /// <summary>Initializes a new instance of the <see cref="ContractsViewModel"/> class.</summary>
    /// <param name="contracts">The contracts of the student.</param>
    public ContractsViewModel(StudentContracts contracts)
    {
        _contracts = contracts;
        RefreshCommand = new Command(async () => await RefreshAsync());
    }

    /// <summary>Gets the contracts.</summary>
    public IReadOnlyList<ContractEntry> Items
    {
        get;
        private set => SetProperty(ref field, value);
    } = [];

    /// <summary>Gets that there are no contracts, or that they are from the device, or <see langword="null"/>.</summary>
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

    /// <summary>Gets or sets a value indicating whether the page is being refreshed by pulling it down.</summary>
    public bool IsRefreshing
    {
        get;
        set => SetProperty(ref field, value);
    }

    /// <summary>Gets the command that reads the contracts again.</summary>
    public Command RefreshCommand { get; }

    /// <summary>Reads the contracts, their cards and the payments.</summary>
    /// <returns>A task that completes when the page shows them.</returns>
    public async Task LoadAsync()
    {
        try
        {
            var contracts = await _contracts.ReadContractsAsync(CancellationToken.None);
            var payments = await _contracts.ReadPaymentsAsync(CancellationToken.None);
            var entries = new List<ContractEntry>();
            var offlineSince = contracts.IsFromCache ? contracts.SavedAt : (DateTimeOffset?)null;
            foreach (var contract in contracts.Value.OrderBy(contract => contract.Status == ContractStatus.Active ? 0 : 1))
            {
                if (contract.Id is not { } contractId)
                    continue;

                var details = await _contracts.ReadDetailsAsync(contractId, CancellationToken.None);
                if (details.IsFromCache && (offlineSince is null || details.SavedAt < offlineSince))
                    offlineSince = details.SavedAt;

                entries.Add(Entry(details.Value, payments.Value.Where(payment => payment.ContractId == contractId)));
            }

            Items = entries;
            Notice = offlineSince is { } savedAt
                ? AppTexts.Format("Contracts.Offline", savedAt.ToLocalTime().ToString("g", CultureInfo.CurrentCulture))
                : entries.Count == 0 ? AppTexts.Get("Contracts.Empty") : null;
        }
        catch (Exception exception) when (exception is ApiException or HttpRequestException)
        {
            Items = [];
            Notice = AppTexts.Get("Contracts.Unavailable");
        }
    }

    private static string MoneyOf(double? amount, string? currency) =>
        string.Create(CultureInfo.CurrentCulture, $"{amount ?? 0:N2} {currency}");

    private static ContractEntry Entry(ContractDetails contract, IEnumerable<PaymentItem> payments)
    {
        var culture = CultureInfo.CurrentCulture;
        var completed = contract.CompletedPracticeHours ?? 0;
        var course = contract.CoursePracticeHours ?? 0;
        var state = contract.Status switch
        {
            ContractStatus.Active => AppTexts.Get("Contracts.Active"),
            ContractStatus.Completed => AppTexts.Get("Contracts.Completed"),
            _ => AppTexts.Get("Contracts.Terminated")
        };
        var debt = contract.DebtAmount ?? 0;

        return new ContractEntry
        {
            Course = contract.CourseName ?? string.Empty,
            Number = LessonTexts.DetailsOf(contract.Number, state),
            Practice = AppTexts.Format("Contracts.Practice", completed.ToString("0.##", culture), course),
            PracticeProgress = course > 0 ? Math.Clamp(completed / course, 0d, 1d) : 0,
            Theory = AppTexts.Format("Contracts.Theory", contract.AttendedTheoryLessons ?? 0),
            Quality = contract.QualityScore is { } quality
                ? AppTexts.Format("Contracts.Quality", quality.ToString("0.0", culture))
                : null,
            Money = debt > 0
                ? AppTexts.Format(
                    "Contracts.MoneyOwed",
                    MoneyOf(contract.PaidAmount, contract.Currency),
                    MoneyOf(contract.PriceAmount, contract.Currency),
                    MoneyOf(debt, contract.Currency))
                : AppTexts.Format(
                    "Contracts.MoneyPaid",
                    MoneyOf(contract.PaidAmount, contract.Currency),
                    MoneyOf(contract.PriceAmount, contract.Currency)),
            Payments = [.. payments.Select(Payment)]
        };
    }

    private static string Payment(PaymentItem payment)
    {
        var state = payment.Status switch
        {
            PaymentStatus.Registered => AppTexts.Get("Payments.Registered"),
            PaymentStatus.Refunded => AppTexts.Get("Payments.Refunded"),
            PaymentStatus.Cancelled => AppTexts.Get("Payments.Cancelled"),
            _ => null
        };

        return LessonTexts.DetailsOf(
            payment.RegisteredAtUtc?.ToLocalTime().ToString("d", CultureInfo.CurrentCulture),
            MoneyOf(payment.Amount, payment.Currency),
            payment.Purpose,
            state);
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
