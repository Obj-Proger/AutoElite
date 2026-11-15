using System.ComponentModel;
using AutoElite.Mobile.Pages;
using Microsoft.Maui.Controls.Shapes;
using ShapePath = Microsoft.Maui.Controls.Shapes.Path;

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
        ["home"] = "M3 10.5 12 3l9 7.5V20a1 1 0 0 1-1 1h-5v-6H9v6H4a1 1 0 0 1-1-1z",
        ["schedule"] = "M5 5h14a2 2 0 0 1 2 2v12a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2V7a2 2 0 0 1 2-2z M3 10h18 M8 3v4 M16 3v4",
        ["study"] = "M2 5h6a4 4 0 0 1 4 4v11a3 3 0 0 0-3-3H2z M22 5h-6a4 4 0 0 0-4 4v11a3 3 0 0 1 3-3h7z",
        ["chats"] = "M21 12a8 8 0 0 1-11.6 7.1L4 20l1-4.6A8 8 0 1 1 21 12z",
        ["profile"] = "M8 8a4 4 0 1 0 8 0a4 4 0 1 0-8 0z M4 21a8 8 0 0 1 16 0"
    };

    private static readonly PathGeometryConverter _geometries = new();

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

    private static Color Resource(string key) =>
        Application.Current?.Resources.TryGetValue(key, out var value) == true && value is Color color ? color : Colors.Gray;

    private void Attach()
    {
        Detach();
        _shell = Shell.Current;
        if (_shell is null)
            return;

        _shell.Navigated += OnNavigated;
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

        foreach (var tab in _watched)
            tab.PropertyChanged -= OnTabChanged;

        _watched.Clear();
        _shell = null;
    }

    private void OnNavigated(object? sender, ShellNavigatedEventArgs args) => Draw();

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
        var color = Resource(isCurrent ? "Accent" : "TextMuted");
        var part = new VerticalStackLayout
        {
            Spacing = 4,
            MinimumHeightRequest = 56,
            Padding = new Thickness(2, 0),
            HorizontalOptions = LayoutOptions.Fill
        };
        if (tab.Route is { } route && _icons.TryGetValue(route, out var icon))
        {
            part.Add(new ShapePath
            {
                Data = (Geometry?)_geometries.ConvertFromInvariantString(icon),
                Stroke = color,
                StrokeThickness = 1.8,
                StrokeLineCap = PenLineCap.Round,
                StrokeLineJoin = PenLineJoin.Round,
                WidthRequest = 24,
                HeightRequest = 24,
                HorizontalOptions = LayoutOptions.Center
            });
        }

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
