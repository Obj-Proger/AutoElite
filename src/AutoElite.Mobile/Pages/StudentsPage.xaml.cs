namespace AutoElite.Mobile.Pages;

/// <summary>The students of a driving instructor and the search of the students for the office, a tab of the shell.</summary>
public partial class StudentsPage : ContentPage
{
    private readonly StudentsViewModel _viewModel;

    /// <summary>Initializes a new instance of the <see cref="StudentsPage"/> class.</summary>
    /// <param name="viewModel">The state of the page.</param>
    public StudentsPage(StudentsViewModel viewModel)
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
