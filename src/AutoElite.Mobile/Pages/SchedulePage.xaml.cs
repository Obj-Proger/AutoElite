namespace AutoElite.Mobile.Pages;

/// <summary>The schedule of the user, a tab of the shell: a list, or a grid of hours for a week on a wide page.</summary>
public partial class SchedulePage : ContentPage
{
    // The width from which the seven columns of a week as a grid of hours stay readable.
    private const double WideWidth = 900;

    private readonly ScheduleViewModel _viewModel;

    /// <summary>Initializes a new instance of the <see cref="SchedulePage"/> class.</summary>
    /// <param name="viewModel">The state of the page.</param>
    public SchedulePage(ScheduleViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    /// <inheritdoc />
    protected override void OnSizeAllocated(double width, double height)
    {
        base.OnSizeAllocated(width, height);
        if (width > 0)
            _viewModel.IsWide = width >= WideWidth;
    }

    /// <inheritdoc />
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.AppearAsync();
    }
}
