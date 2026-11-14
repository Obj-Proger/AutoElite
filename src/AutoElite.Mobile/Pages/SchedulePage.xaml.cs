namespace AutoElite.Mobile.Pages;

/// <summary>The schedule of the user as a grid of hours, a tab of the shell.</summary>
public partial class SchedulePage : ContentPage
{
    private readonly ScheduleViewModel _viewModel;

    /// <summary>Initializes a new instance of the <see cref="SchedulePage"/> class.</summary>
    /// <param name="viewModel">The state of the page.</param>
    public SchedulePage(ScheduleViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    /// <inheritdoc />
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.AppearAsync();
    }
}
