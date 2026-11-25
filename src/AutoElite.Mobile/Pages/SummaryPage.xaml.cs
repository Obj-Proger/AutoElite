namespace AutoElite.Mobile.Pages;

/// <summary>The summary of the school, opened from the home page of the office.</summary>
public partial class SummaryPage : ContentPage
{
    private readonly SummaryViewModel _viewModel;

    /// <summary>Initializes a new instance of the <see cref="SummaryPage"/> class.</summary>
    /// <param name="viewModel">The state of the page.</param>
    public SummaryPage(SummaryViewModel viewModel)
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
