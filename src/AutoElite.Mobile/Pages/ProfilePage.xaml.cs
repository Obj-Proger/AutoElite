namespace AutoElite.Mobile.Pages;

/// <summary>The profile of the user with the settings of the application, a tab of the shell.</summary>
public partial class ProfilePage : ContentPage
{
    private readonly ProfileViewModel _viewModel;

    /// <summary>Initializes a new instance of the <see cref="ProfilePage"/> class.</summary>
    /// <param name="viewModel">The state of the page.</param>
    public ProfilePage(ProfileViewModel viewModel)
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
