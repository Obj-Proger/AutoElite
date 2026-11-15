namespace AutoElite.Mobile.Pages;

/// <summary>An exam with the members of its group and their results, opened by an instructor from the schedule.</summary>
public partial class ExamPage : ContentPage
{
    private readonly ExamViewModel _viewModel;

    /// <summary>Initializes a new instance of the <see cref="ExamPage"/> class.</summary>
    /// <param name="viewModel">The state of the page.</param>
    public ExamPage(ExamViewModel viewModel)
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
