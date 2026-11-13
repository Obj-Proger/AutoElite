namespace AutoElite.Mobile.Pages;

/// <summary>The settings, opened from the home page.</summary>
public partial class SettingsPage : ContentPage
{
    /// <summary>Initializes a new instance of the <see cref="SettingsPage"/> class.</summary>
    /// <param name="viewModel">The state of the page.</param>
    public SettingsPage(SettingsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
