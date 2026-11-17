using System.Globalization;
using AutoElite.Client.Api.Models;
using AutoElite.Mobile.Localization;
using AutoElite.Mobile.Schedule;
using Microsoft.Kiota.Abstractions;

namespace AutoElite.Mobile.Pages;

/// <summary>
/// A payment that a manager records towards a contract: the amount, offered as what is owed, how the student paid, what
/// for, and whether the money is received, in which case it counts towards the debt at once; otherwise the payment waits
/// for its confirmation on the card of the student. Recording needs the network.
/// </summary>
public sealed class PaymentViewModel : ViewModel, IQueryAttributable
{
    /// <summary>The name of the parameter with the <see cref="PaymentRequest"/>.</summary>
    public const string ContractParameter = "contract";

    // The ways of paying at the office; an online payment comes through the payment gateway.
    private static readonly PaymentMethod[] _methods = [PaymentMethod.Cash, PaymentMethod.BankCard, PaymentMethod.BankTransfer];

    private readonly StudentDirectory _directory;
    private PaymentRequest? _request;

    /// <summary>Initializes a new instance of the <see cref="PaymentViewModel"/> class.</summary>
    /// <param name="directory">The students of the school and their contracts.</param>
    public PaymentViewModel(StudentDirectory directory)
    {
        _directory = directory;
        SaveCommand = new Command(async () => await SaveAsync(), () => !IsBusy);
        Purpose = AppTexts.Get("Payment.DefaultPurpose");
    }

    /// <summary>Gets the number and the course of the contract.</summary>
    public string Contract
    {
        get;
        private set => SetProperty(ref field, value);
    } = string.Empty;

    /// <summary>Gets or sets the amount as typed.</summary>
    public string Amount
    {
        get;
        set => SetProperty(ref field, value);
    } = string.Empty;

    /// <summary>Gets the currency of the contract.</summary>
    public string Currency
    {
        get;
        private set => SetProperty(ref field, value);
    } = string.Empty;

    /// <summary>Gets the ways of paying, in the language of the user.</summary>
    public IReadOnlyList<string> Methods { get; } =
        [AppTexts.Get("Payment.Cash"), AppTexts.Get("Payment.BankCard"), AppTexts.Get("Payment.BankTransfer")];

    /// <summary>Gets or sets the position of the way of paying chosen.</summary>
    public int MethodIndex
    {
        get;
        set => SetProperty(ref field, value);
    }

    /// <summary>Gets or sets what the payment is for.</summary>
    public string Purpose
    {
        get;
        set => SetProperty(ref field, value);
    } = string.Empty;

    /// <summary>Gets or sets a value indicating whether the money is received, so that the payment counts at once.</summary>
    public bool IsReceived
    {
        get;
        set => SetProperty(ref field, value);
    } = true;

    /// <summary>Gets why the payment cannot be recorded.</summary>
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

    /// <summary>Gets a value indicating whether the payment is being recorded.</summary>
    public bool IsBusy
    {
        get;
        private set
        {
            if (SetProperty(ref field, value))
                SaveCommand.ChangeCanExecute();
        }
    }

    /// <summary>Gets the command that records the payment and closes the page.</summary>
    public Command SaveCommand { get; }

    /// <inheritdoc />
    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        ArgumentNullException.ThrowIfNull(query);
        if (!query.TryGetValue(ContractParameter, out var value) || value is not PaymentRequest request)
            return;

        _request = request;
        Contract = request.Title;
        Currency = request.Currency ?? string.Empty;
        Amount = request.Debt > 0 ? request.Debt.ToString("0.##", CultureInfo.CurrentCulture) : string.Empty;
        Message = null;
    }

    private static bool TryParseAmount(string text, out double amount)
    {
        var parsed = double.TryParse(text, NumberStyles.Number, CultureInfo.CurrentCulture, out amount)
            || double.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out amount);
        amount = Math.Round(amount, 2);

        return parsed && amount > 0;
    }

    private async Task SaveAsync()
    {
        if (_request is not { } request)
            return;

        if (!TryParseAmount(Amount, out var amount))
        {
            Message = AppTexts.Get("Payment.WrongAmount");

            return;
        }

        IsBusy = true;
        Message = null;
        try
        {
            var method = _methods[Math.Clamp(MethodIndex, 0, _methods.Length - 1)];
            await _directory.RegisterPaymentAsync(
                request.ContractId, amount, method, Purpose.Trim(), IsReceived, CancellationToken.None);
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
