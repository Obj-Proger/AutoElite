namespace AutoElite.Mobile.Pages;

/// <summary>A practice of the questions, question by question, opened from the hub of the practice.</summary>
public partial class PracticeSessionPage : ContentPage
{
    private readonly PracticeSessionViewModel _viewModel;

    /// <summary>Initializes a new instance of the <see cref="PracticeSessionPage"/> class.</summary>
    /// <param name="viewModel">The state of the page.</param>
    public PracticeSessionPage(PracticeSessionViewModel viewModel)
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
