namespace AutoElite.Mobile.Pages;

/// <summary>A chat, opened from the chats page; it keeps the latest message in view.</summary>
public partial class ChatPage : ContentPage
{
    private readonly ChatViewModel _viewModel;

    /// <summary>Initializes a new instance of the <see cref="ChatPage"/> class.</summary>
    /// <param name="viewModel">The state of the page.</param>
    public ChatPage(ChatViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
        _viewModel.PropertyChanged += async (_, args) =>
        {
            if (args.PropertyName == nameof(ChatViewModel.Messages))
                await Scroller.ScrollToAsync(MessageList, ScrollToPosition.End, animated: false);
        };
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
