namespace AutoElite.Mobile.Pages;

/// <summary>The home page of the signed-in user.</summary>
public partial class HomePage : ContentPage
{
    private readonly HomeViewModel _viewModel;

    /// <summary>Initializes a new instance of the <see cref="HomePage"/> class.</summary>
    /// <param name="viewModel">The state of the page.</param>
    public HomePage(HomeViewModel viewModel)
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
