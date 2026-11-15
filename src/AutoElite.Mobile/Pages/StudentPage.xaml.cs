namespace AutoElite.Mobile.Pages;

/// <summary>The progress of a student, opened by an instructor from a lesson or a chat.</summary>
public partial class StudentPage : ContentPage
{
    private readonly StudentViewModel _viewModel;

    /// <summary>Initializes a new instance of the <see cref="StudentPage"/> class.</summary>
    /// <param name="viewModel">The state of the page.</param>
    public StudentPage(StudentViewModel viewModel)
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
