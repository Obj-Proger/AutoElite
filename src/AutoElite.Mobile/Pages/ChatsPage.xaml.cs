namespace AutoElite.Mobile.Pages;

/// <summary>The chats of the user, opened from the home page.</summary>
public partial class ChatsPage : ContentPage
{
    private readonly ChatsViewModel _viewModel;

    /// <summary>Initializes a new instance of the <see cref="ChatsPage"/> class.</summary>
    /// <param name="viewModel">The state of the page.</param>
    public ChatsPage(ChatsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    /// <inheritdoc />
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        _viewModel.StartListening();
        await _viewModel.LoadAsync();
    }

    /// <inheritdoc />
    protected override void OnDisappearing()
    {
        _viewModel.StopListening();
        base.OnDisappearing();
    }
}
