using System.ComponentModel;
using AutoElite.Mobile.Pages;
using AutoElite.Mobile.Theming;

namespace AutoElite.Mobile.Controls;

/// <summary>
/// The tabs of the signed-in user as a strip of equal parts, each an icon over its caption, so that every tab stays in
/// view at any width of the window. On Windows it takes the place of the tabs of the shell, which hide the tabs that do
/// not fit behind a menu; it shows the tabs of the shell that are visible, marks the current one and opens the one
/// tapped.
/// </summary>
public sealed class AppTabs : ContentView
{
    // The icons of the tabs by their routes, the same drawings as the images of the shell (Resources/Images).
    private static readonly Dictionary<string, string> _icons = new(StringComparer.Ordinal)
    {
        ["home"] = "home",
        ["schedule"] = "calendar",
        ["study"] = "book-open",
        ["students"] = "users",
        ["chats"] = "message-circle",
        ["profile"] = "user"
    };

    private readonly Grid _strip = new() { Padding = new Thickness(8, 8, 8, 0) };
    private readonly List<ShellSection> _watched = [];
    private Shell? _shell;

    /// <summary>Initializes a new instance of the <see cref="AppTabs"/> class.</summary>
    public AppTabs()
    {
        Content = _strip;
        Loaded += (_, _) => Attach();
        Unloaded += (_, _) => Detach();
    }

    // The tabs of the signed-in user: the items of the tab bar of the shell.
    private TabBar? Bar => _shell?.Items.OfType<TabBar>().FirstOrDefault();

    private static Color Resource(string key) => AppPalette.ColorOf(key);

    private void Attach()
    {
        Detach();
        _shell = Shell.Current;
        if (_shell is null)
            return;

        _shell.Navigated += OnNavigated;
        AppPalette.Changed += OnPaletteChanged;
        foreach (var tab in Bar?.Items ?? [])
        {
            tab.PropertyChanged += OnTabChanged;
            _watched.Add(tab);
        }

        Draw();
    }

    private void Detach()
    {
        if (_shell is not null)
            _shell.Navigated -= OnNavigated;

        AppPalette.Changed -= OnPaletteChanged;

        foreach (var tab in _watched)
            tab.PropertyChanged -= OnTabChanged;

        _watched.Clear();
        _shell = null;
    }

    private void OnNavigated(object? sender, ShellNavigatedEventArgs args) => Draw();

    // The colours are taken when the parts are made, so a new theme makes them again.
    private void OnPaletteChanged(object? sender, EventArgs args) => Dispatcher.Dispatch(Draw);

    // A tab shown or hidden for the roles of the user, or its caption in another language.
    private void OnTabChanged(object? sender, PropertyChangedEventArgs args)
    {
        if (args.PropertyName is nameof(ShellSection.IsVisible) or nameof(ShellSection.Title))
            Dispatcher.Dispatch(Draw);
    }

    private void Draw()
    {
        _strip.Children.Clear();
        _strip.ColumnDefinitions.Clear();
        if (Bar is not { } bar)
            return;

        var tabs = bar.Items.Where(tab => tab.IsVisible).ToList();
        for (var column = 0; column < tabs.Count; column++)
        {
            _strip.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
            var part = Part(bar, tabs[column], tabs[column] == bar.CurrentItem);
            _strip.Add(part, column, 0);
        }
    }

    // One tab: its icon over its caption and, under the current one, a short bar.
    private static VerticalStackLayout Part(TabBar bar, ShellSection tab, bool isCurrent)
    {
        var color = Resource(isCurrent ? "AccentText" : "TextMuted");
        var part = new VerticalStackLayout
        {
            Spacing = 4,
            MinimumHeightRequest = 56,
            Padding = new Thickness(2, 0),
            HorizontalOptions = LayoutOptions.Fill
        };
        if (tab.Route is { } route && _icons.TryGetValue(route, out var icon))
            part.Add(new AppIcon { Name = icon, Color = color });

        part.Add(new Label
        {
            Text = tab.Title,
            TextColor = color,
            FontSize = 13,
            MaxLines = 1,
            LineBreakMode = LineBreakMode.TailTruncation,
            HorizontalTextAlignment = TextAlignment.Center,
            HorizontalOptions = LayoutOptions.Fill
        });
        part.Add(new BoxView
        {
            Color = isCurrent ? color : Colors.Transparent,
            WidthRequest = 28,
            HeightRequest = 3,
            CornerRadius = 1.5,
            HorizontalOptions = LayoutOptions.Center
        });

        SemanticProperties.SetDescription(part, tab.Title);
        part.GestureRecognizers.Add(new TapGestureRecognizer
        {
            Command = new Command(async () => await Routes.GoToAsync($"{bar.Route}/{tab.Route}"))
        });

        return part;
    }
}
