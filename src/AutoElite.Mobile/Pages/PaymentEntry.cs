using System.Windows.Input;

namespace AutoElite.Mobile.Pages;

/// <summary>A payment of a contract for a manager, who confirms one that waits for its money.</summary>
public sealed class PaymentEntry
{
    /// <summary>Gets the day, the amount, the purpose and the state of the payment.</summary>
    public required string Text { get; init; }

    /// <summary>Gets the command that confirms the payment, or <see langword="null"/> for one that needs no confirmation.</summary>
    public ICommand? ConfirmCommand { get; init; }

    /// <summary>Gets a value indicating whether the payment waits for its confirmation.</summary>
    public bool CanConfirm => ConfirmCommand is not null;
}
