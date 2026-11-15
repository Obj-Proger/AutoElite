using AutoElite.Client.Api.Models;

namespace AutoElite.Mobile.Pages;

/// <summary>A licence category the question being written may apply to, which the instructor turns on or off.</summary>
public sealed class CategoryEntry : ViewModel
{
    /// <summary>Initializes a new instance of the <see cref="CategoryEntry"/> class.</summary>
    /// <param name="category">The category.</param>
    public CategoryEntry(LicenseCategory category)
    {
        Category = category;
        Name = CategoryNames.Of(category);
        ToggleCommand = new Command(() => IsSelected = !IsSelected);
    }

    /// <summary>Gets the category.</summary>
    public LicenseCategory Category { get; }

    /// <summary>Gets the name of the category.</summary>
    public string Name { get; }

    /// <summary>Gets or sets a value indicating whether the question applies to the category.</summary>
    public bool IsSelected
    {
        get;
        set => SetProperty(ref field, value);
    }

    /// <summary>Gets the command that turns the category on or off.</summary>
    public Command ToggleCommand { get; }
}
