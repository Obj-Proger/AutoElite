namespace AutoElite.Mobile.Pages;

/// <summary>A lead for a manager, opened over the leads.</summary>
public partial class LeadPage : ContentPage
{
    private readonly LeadViewModel _viewModel;

    /// <summary>Initializes a new instance of the <see cref="LeadPage"/> class.</summary>
    /// <param name="viewModel">The state of the page.</param>
    public LeadPage(LeadViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    /// <inheritdoc />
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadAsync();
    }
}
