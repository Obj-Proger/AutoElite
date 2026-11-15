namespace AutoElite.Mobile.Pages;

/// <summary>The people to start a personal chat with, opened over the tab of chats.</summary>
public partial class NewChatPage : ContentPage
{
    private readonly NewChatViewModel _viewModel;

    /// <summary>Initializes a new instance of the <see cref="NewChatPage"/> class.</summary>
    /// <param name="viewModel">The state of the page.</param>
    public NewChatPage(NewChatViewModel viewModel)
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
