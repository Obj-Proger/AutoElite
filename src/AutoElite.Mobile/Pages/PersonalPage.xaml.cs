namespace AutoElite.Mobile.Pages;

/// <summary>The personal details of the user, opened from the profile.</summary>
public partial class PersonalPage : ContentPage
{
    private readonly PersonalViewModel _viewModel;

    /// <summary>Initializes a new instance of the <see cref="PersonalPage"/> class.</summary>
    /// <param name="viewModel">The state of the page.</param>
    public PersonalPage(PersonalViewModel viewModel)
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
