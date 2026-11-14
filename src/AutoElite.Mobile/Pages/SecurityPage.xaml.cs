namespace AutoElite.Mobile.Pages;

/// <summary>The change of the password and of the email address, opened over the profile.</summary>
public partial class SecurityPage : ContentPage
{
    /// <summary>Initializes a new instance of the <see cref="SecurityPage"/> class.</summary>
    /// <param name="viewModel">The state of the page.</param>
    public SecurityPage(SecurityViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
