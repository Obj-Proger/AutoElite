namespace AutoElite.Mobile.Pages;

/// <summary>The notifications of the user, opened from the home page.</summary>
public partial class NotificationsPage : ContentPage
{
    private readonly NotificationsViewModel _viewModel;

    /// <summary>Initializes a new instance of the <see cref="NotificationsPage"/> class.</summary>
    /// <param name="viewModel">The state of the page.</param>
    public NotificationsPage(NotificationsViewModel viewModel)
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
