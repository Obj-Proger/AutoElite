using System.ComponentModel;

namespace AutoElite.Mobile.Pages;

/// <summary>
/// A conversation, on the page of a chat or in the pane of the chats page on a wide window; it keeps the latest message
/// in view.
/// </summary>
public partial class ChatView : ContentView
{
    /// <summary>Identifies the <see cref="ShowsHeader"/> property.</summary>
    public static readonly BindableProperty ShowsHeaderProperty = BindableProperty.Create(
        nameof(ShowsHeader),
        typeof(bool),
        typeof(ChatView),
        false,
        propertyChanged: (bindable, _, value) => ((ChatView)bindable).Header.IsVisible = (bool)value);

    private ChatViewModel? _viewModel;

    /// <summary>Initializes a new instance of the <see cref="ChatView"/> class.</summary>
    public ChatView() => InitializeComponent();

    /// <summary>
    /// Gets or sets a value indicating whether the conversation has a header of its own with the person or the group and
    /// what can be done with the chat, as in a pane, which has no bar of a page.
    /// </summary>
    public bool ShowsHeader
    {
        get => (bool)GetValue(ShowsHeaderProperty);
        set => SetValue(ShowsHeaderProperty, value);
    }

    /// <inheritdoc />
    protected override void OnBindingContextChanged()
    {
        base.OnBindingContextChanged();
        if (_viewModel is not null)
            _viewModel.PropertyChanged -= OnViewModelChanged;

        _viewModel = BindingContext as ChatViewModel;
        if (_viewModel is not null)
            _viewModel.PropertyChanged += OnViewModelChanged;
    }

    private async void OnViewModelChanged(object? sender, PropertyChangedEventArgs args)
    {
        if (args.PropertyName == nameof(ChatViewModel.Messages))
            await Scroller.ScrollToAsync(MessageList, ScrollToPosition.End, animated: false);
    }
}
