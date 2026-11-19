namespace AutoElite.Mobile.Pages;

/// <summary>The leads of a manager or an administrator, a tab.</summary>
public partial class LeadsPage : ContentPage
{
    private readonly LeadsViewModel _viewModel;

    /// <summary>Initializes a new instance of the <see cref="LeadsPage"/> class.</summary>
    /// <param name="viewModel">The state of the page.</param>
    public LeadsPage(LeadsViewModel viewModel)
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
