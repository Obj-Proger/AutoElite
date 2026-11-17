using System.Windows.Input;

namespace AutoElite.Mobile.Pages;

/// <summary>A contract of a student for a manager: its money first, then the progress of the training and its payments.</summary>
public sealed class ManagedContractEntry
{
    /// <summary>Gets the course with the category or the gearbox.</summary>
    public required string Course { get; init; }

    /// <summary>Gets the number, the state and the dates of the contract.</summary>
    public required string Number { get; init; }

    /// <summary>Gets what was paid out of the price, and what is still owed.</summary>
    public required string Money { get; init; }

    /// <summary>Gets a value indicating whether the student still owes money on the contract.</summary>
    public bool HasDebt { get; init; }

    /// <summary>Gets the driving hours completed out of those of the course.</summary>
    public required string Practice { get; init; }

    /// <summary>Gets the share of the driving hours completed, from 0 to 1.</summary>
    public double PracticeProgress { get; init; }

    /// <summary>Gets the theory lessons attended, with the group if the contract is enrolled in one.</summary>
    public required string Theory { get; init; }

    /// <summary>Gets the payments of the contract, the latest first.</summary>
    public IReadOnlyList<PaymentEntry> Payments { get; init; } = [];

    /// <summary>Gets a value indicating whether the contract has payments.</summary>
    public bool HasPayments => Payments.Count > 0;

    /// <summary>Gets the command that records a payment towards the contract, or <see langword="null"/> for a closed one.</summary>
    public ICommand? AddPaymentCommand { get; init; }

    /// <summary>Gets a value indicating whether a payment can be recorded towards the contract.</summary>
    public bool CanAddPayment => AddPaymentCommand is not null;
}
