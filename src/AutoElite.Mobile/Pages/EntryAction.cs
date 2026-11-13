using System.Windows.Input;

namespace AutoElite.Mobile.Pages;

/// <summary>Something the user can do with a lesson, shown as a button on its card.</summary>
/// <param name="Text">The text of the button.</param>
/// <param name="Command">What the button does.</param>
public sealed record EntryAction(string Text, ICommand Command);
