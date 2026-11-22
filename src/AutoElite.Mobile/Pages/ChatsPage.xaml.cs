namespace AutoElite.Mobile.Pages;

/// <summary>The chats of the user, a tab of the shell: a list or, on a wide window, the list with the open chat next to it.</summary>
public partial class ChatsPage : ContentPage
{
    // The width from which a chat has room next to the list.
    private const double WideWidth = 840;

    // The width of the list next to a chat.
    private const double ListWidth = 340;

    private readonly ChatsViewModel _viewModel;

    /// <summary>Initializes a new instance of the <see cref="ChatsPage"/> class.</summary>
    /// <param name="viewModel">The state of the page.</param>
    public ChatsPage(ChatsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    /// <inheritdoc />
    protected override void OnSizeAllocated(double width, double height)
    {
        base.OnSizeAllocated(width, height);
        if (width <= 0)
            return;

        var isWide = width >= WideWidth;
        Panes.ColumnDefinitions[0].Width = isWide ? new GridLength(ListWidth) : GridLength.Star;
        Panes.ColumnDefinitions[1].Width = isWide ? GridLength.Star : new GridLength(0);
        Pane.IsVisible = isWide;
        _viewModel.IsWide = isWide;
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
