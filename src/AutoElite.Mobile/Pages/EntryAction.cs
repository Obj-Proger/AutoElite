using System.Windows.Input;

namespace AutoElite.Mobile.Pages;

/// <summary>Something the user can do with a lesson, shown as a button on its card.</summary>
/// <param name="Text">The text of the button.</param>
/// <param name="Command">What the button does.</param>
/// <param name="IsMain">
/// Whether it is what the lesson is there for, such as completing it or marking the attendance, which a highlighted card
/// shows as its main button; cancelling, for one, is not.
/// </param>
public sealed record EntryAction(string Text, ICommand Command, bool IsMain = false);
