namespace AutoElite.Mobile.Pages;

/// <summary>A practice test attempt, opened from the tests page; it shows the time left once a second.</summary>
public partial class TestSessionPage : ContentPage
{
    private readonly TestSessionViewModel _viewModel;
    private readonly IDispatcherTimer _timer;

    /// <summary>Initializes a new instance of the <see cref="TestSessionPage"/> class.</summary>
    /// <param name="viewModel">The state of the page.</param>
    public TestSessionPage(TestSessionViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
        _timer = Dispatcher.CreateTimer();
        _timer.Interval = TimeSpan.FromSeconds(1);
        _timer.Tick += async (_, _) => await _viewModel.TickAsync();
    }

    /// <inheritdoc />
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadAsync();
        _timer.Start();
    }

    /// <inheritdoc />
    protected override void OnDisappearing()
    {
        _timer.Stop();
        base.OnDisappearing();
    }
}
