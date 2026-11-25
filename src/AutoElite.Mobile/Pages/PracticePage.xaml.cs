namespace AutoElite.Mobile.Pages;

/// <summary>The hub of the practice of the questions, opened from the studies and the tests of a student.</summary>
public partial class PracticePage : ContentPage
{
    private readonly PracticeViewModel _viewModel;

    /// <summary>Initializes a new instance of the <see cref="PracticePage"/> class.</summary>
    /// <param name="viewModel">The state of the page.</param>
    public PracticePage(PracticeViewModel viewModel)
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
