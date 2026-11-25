using System.Windows.Input;
using AutoElite.Mobile.Controls;

namespace AutoElite.Mobile.Pages;

/// <summary>A payment that waits for its confirmation, or a contract whose student owes money, as a row.</summary>
public sealed class MoneyEntry
{
    /// <summary>Gets the name of the student.</summary>
    public required string Title { get; init; }

    /// <summary>Gets the contract and the details of the payment or the course.</summary>
    public required string Subtitle { get; init; }

    /// <summary>Gets the amount of the payment or the debt.</summary>
    public required string Amount { get; init; }

    /// <summary>Gets a line under the amount, such as the price of the contract.</summary>
    public string? Detail { get; init; }

    /// <summary>Gets the tone of the row: a warning for a waiting payment, danger for a debt.</summary>
    public StatusTone Tone { get; init; }

    /// <summary>Gets the command of the row: the actions of a payment, or the card of the student.</summary>
    public required ICommand Command { get; init; }
}
