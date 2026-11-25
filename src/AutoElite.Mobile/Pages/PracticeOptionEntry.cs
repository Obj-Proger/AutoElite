using System.Windows.Input;

namespace AutoElite.Mobile.Pages;

/// <summary>An answer option of a question in practice: tappable until the question is answered, then marked.</summary>
/// <param name="Text">The text of the option.</param>
/// <param name="Command">What tapping the option does: answers with it, or nothing once the question is answered.</param>
/// <param name="IsRight">Whether the question is answered and this is the correct option.</param>
/// <param name="IsWrong">Whether the question is answered with this option and it is wrong.</param>
public sealed record PracticeOptionEntry(string Text, ICommand? Command, bool IsRight, bool IsWrong);
