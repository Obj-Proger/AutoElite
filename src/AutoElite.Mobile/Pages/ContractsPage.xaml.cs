namespace AutoElite.Mobile.Pages;

/// <summary>The contracts of a student, opened from their home page.</summary>
public partial class ContractsPage : ContentPage
{
    private readonly ContractsViewModel _viewModel;

    /// <summary>Initializes a new instance of the <see cref="ContractsPage"/> class.</summary>
    /// <param name="viewModel">The state of the page.</param>
    public ContractsPage(ContractsViewModel viewModel)
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
