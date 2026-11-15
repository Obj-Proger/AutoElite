namespace AutoElite.Mobile.Pages;

/// <summary>A question of the school to write or correct, opened over the questions.</summary>
public partial class QuestionPage : ContentPage
{
    private readonly QuestionViewModel _viewModel;

    /// <summary>Initializes a new instance of the <see cref="QuestionPage"/> class.</summary>
    /// <param name="viewModel">The state of the page.</param>
    public QuestionPage(QuestionViewModel viewModel)
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
