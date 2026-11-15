namespace AutoElite.Mobile.Pages;

/// <summary>The questions of the school for the practice tests, opened from the tab of studies of a theory instructor.</summary>
public partial class QuestionsPage : ContentPage
{
    private readonly QuestionsViewModel _viewModel;

    /// <summary>Initializes a new instance of the <see cref="QuestionsPage"/> class.</summary>
    /// <param name="viewModel">The state of the page.</param>
    public QuestionsPage(QuestionsViewModel viewModel)
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
