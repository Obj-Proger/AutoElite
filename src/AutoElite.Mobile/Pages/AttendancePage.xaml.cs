namespace AutoElite.Mobile.Pages;

/// <summary>The attendance at a theory lesson, opened from the home page of a theory instructor.</summary>
public partial class AttendancePage : ContentPage
{
    private readonly AttendanceViewModel _viewModel;

    /// <summary>Initializes a new instance of the <see cref="AttendancePage"/> class.</summary>
    /// <param name="viewModel">The state of the page.</param>
    public AttendancePage(AttendanceViewModel viewModel)
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
