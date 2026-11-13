namespace AutoElite.Mobile.Pages;

/// <summary>The practice tests of a student, opened from their home page.</summary>
public partial class TestsPage : ContentPage
{
    private readonly TestsViewModel _viewModel;

    /// <summary>Initializes a new instance of the <see cref="TestsPage"/> class.</summary>
    /// <param name="viewModel">The state of the page.</param>
    public TestsPage(TestsViewModel viewModel)
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
