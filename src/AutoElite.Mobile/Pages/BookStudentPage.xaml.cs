namespace AutoElite.Mobile.Pages;

/// <summary>The booking of a student on a free practice slot by a manager, opened over the schedule.</summary>
public partial class BookStudentPage : ContentPage
{
    private readonly BookStudentViewModel _viewModel;

    /// <summary>Initializes a new instance of the <see cref="BookStudentPage"/> class.</summary>
    /// <param name="viewModel">The state of the page.</param>
    public BookStudentPage(BookStudentViewModel viewModel)
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
