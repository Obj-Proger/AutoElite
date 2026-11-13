namespace AutoElite.Mobile.Pages;

/// <summary>Signing in to the school.</summary>
public partial class SignInPage : ContentPage
{
    private readonly SignInViewModel _viewModel;

    /// <summary>Initializes a new instance of the <see cref="SignInPage"/> class.</summary>
    /// <param name="viewModel">The state of the page.</param>
    public SignInPage(SignInViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    /// <inheritdoc />
    protected override void OnAppearing()
    {
        base.OnAppearing();
        _viewModel.Refresh();
    }
}
