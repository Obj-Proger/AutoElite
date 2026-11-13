using System.Windows.Input;

namespace AutoElite.Mobile.Pages;

/// <summary>An active contract of the student that the booking page offers to book a lesson for.</summary>
public sealed class ContractOption
{
    /// <summary>Gets the course of the contract.</summary>
    public required string Course { get; init; }

    /// <summary>Gets the number of the contract.</summary>
    public required string Number { get; init; }

    /// <summary>Gets the command that shows the free slots of the contract.</summary>
    public required ICommand SelectCommand { get; init; }
}
