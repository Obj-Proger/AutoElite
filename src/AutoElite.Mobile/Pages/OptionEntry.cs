namespace AutoElite.Mobile.Pages;

/// <summary>An answer option of the question being written, which the instructor types and marks correct.</summary>
public sealed class OptionEntry : ViewModel
{
    /// <summary>Gets or sets the text of the option.</summary>
    public string Text
    {
        get;
        set => SetProperty(ref field, value);
    } = string.Empty;

    /// <summary>Gets or sets a value indicating whether this is the correct answer.</summary>
    public bool IsCorrect
    {
        get;
        set => SetProperty(ref field, value);
    }

    /// <summary>Gets or sets the number of the option, from 1.</summary>
    public string Number
    {
        get;
        set => SetProperty(ref field, value);
    } = string.Empty;

    /// <summary>Gets or sets the command that makes this option the correct answer.</summary>
    public Command? CorrectCommand { get; set; }

    /// <summary>Gets or sets the command that removes the option.</summary>
    public Command? RemoveCommand { get; set; }
}
