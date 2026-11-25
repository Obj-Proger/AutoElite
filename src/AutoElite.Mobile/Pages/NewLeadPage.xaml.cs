namespace AutoElite.Mobile.Pages;

/// <summary>A lead that a manager records from a call or a visit, opened over the leads.</summary>
public partial class NewLeadPage : ContentPage
{
    /// <summary>Initializes a new instance of the <see cref="NewLeadPage"/> class.</summary>
    /// <param name="viewModel">The state of the page.</param>
    public NewLeadPage(NewLeadViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
