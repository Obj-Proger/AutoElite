using System.ComponentModel;
using System.Globalization;
using AutoElite.Mobile.Localization;
using AutoElite.Mobile.Pages;
using AutoElite.Mobile.Session;
using AutoElite.Mobile.Theming;
using Microsoft.Maui.Controls.Shapes;

namespace AutoElite.Mobile.Controls;

/// <summary>
/// The frame around a tab, the same on every platform: at the top the title of the page, the bell with the count of
/// unread notifications and the avatar that opens the profile; and the tabs of the user, which follow the width of the
/// window: a bar at the bottom of a phone, a rail of icons at the left of a narrow window and a menu with captions at
/// the left of a wide one. The tabs are those of the tab bar of the shell that are visible, with the counts of unread
/// messages and new leads.
/// </summary>
[ContentProperty(nameof(Body))]
public sealed class AppFrame : Grid
{
    /// <summary>Identifies the <see cref="Body"/> property.</summary>
    public static readonly BindableProperty BodyProperty = BindableProperty.Create(
        nameof(Body), typeof(View), typeof(AppFrame), propertyChanged: OnBodyChanged);

    // The widths from which the tabs move from the bottom to a rail, and from a rail to a menu.
    private const double RailWidth = 600;
    private const double MenuWidth = 1000;

    // The icons of the tabs by their routes.
    private static readonly Dictionary<string, string> _icons = new(StringComparer.Ordinal)
    {
        ["home"] = "home",
        ["schedule"] = "calendar",
        ["study"] = "book-open",
        ["students"] = "users",
        ["leads"] = "inbox",
        ["chats"] = "message-circle"
    };

    private readonly Grid _column = new()
    {
        RowDefinitions = { new RowDefinition(GridLength.Auto), new RowDefinition(GridLength.Star), new RowDefinition(GridLength.Auto) }
    };
    private readonly ContentView _side = new();
    private readonly ContentView _bottom = new();
    private readonly Label _title = new() { FontFamily = "DisplaySemiBold", FontSize = 22, LineBreakMode = LineBreakMode.TailTruncation };
    private readonly Label _subtitle = new() { FontSize = 13 };
    private readonly CountBadge _bellBadge = new() { HorizontalOptions = LayoutOptions.End, VerticalOptions = LayoutOptions.Start };
    private readonly Avatar _avatar = new() { Size = 36, IsAccent = true };
    private readonly List<ShellSection> _watched = [];
    private AppChrome? _chrome;
    private Shell? _shell;
    private NavigationMode _mode = NavigationMode.None;

    /// <summary>Initializes a new instance of the <see cref="AppFrame"/> class.</summary>
    public AppFrame()
    {
        ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
        ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        SetDynamicResource(BackgroundColorProperty, "Background");
        _subtitle.SetDynamicResource(Label.TextColorProperty, "TextMuted");

        _column.Add(TopBar(), 0, 0);
        _column.Add(_bottom, 0, 2);
        this.Add(_side, 0, 0);
        this.Add(_column, 1, 0);

        SizeChanged += (_, _) => Arrange();
        Loaded += (_, _) => Attach();
        Unloaded += (_, _) => Detach();
        AppPalette.Follow(this, DrawTabs);
    }

    // Where the tabs are.
    private enum NavigationMode
    {
        None,
        Bottom,
        Rail,
        Menu
    }

    /// <summary>Gets or sets the page inside the frame.</summary>
    public View? Body
    {
        get => (View?)GetValue(BodyProperty);
        set => SetValue(BodyProperty, value);
    }

    // The tabs of the signed-in user: the items of the tab bar of the shell.
    private TabBar? Bar => _shell?.Items.OfType<TabBar>().FirstOrDefault();

    private static void OnBodyChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var frame = (AppFrame)bindable;
        if (oldValue is View old)
            frame._column.Remove(old);

        if (newValue is View body)
            frame._column.Add(body, 0, 1);
    }

    private static Color ColorOf(string key) => AppPalette.ColorOf(key);

    // A tap on a view runs an action.
    private static void OnTap(View view, Func<Task> action) =>
        view.GestureRecognizers.Add(new TapGestureRecognizer { Command = new Command(async () => await action()) });

    private Grid TopBar()
    {
        var titles = new VerticalStackLayout { Spacing = 2, VerticalOptions = LayoutOptions.Center };
        titles.Add(_title);
        titles.Add(_subtitle);

        var bell = new Grid { WidthRequest = 44, HeightRequest = 44, VerticalOptions = LayoutOptions.Center };
        bell.Add(new AppIcon { Name = "bell", Size = 22 });
        bell.Add(_bellBadge);
        SemanticProperties.SetDescription(bell, AppTexts.Get("Notifications.Title"));
        OnTap(bell, () => Routes.OpenAsync(Routes.Notifications));

        var avatar = new ContentView { Padding = 4, Content = _avatar, VerticalOptions = LayoutOptions.Center };
        SemanticProperties.SetDescription(avatar, AppTexts.Get("Tab.Profile"));
        OnTap(avatar, () => Routes.OpenAsync(Routes.Profile));

        var bar = new Grid
        {
            HeightRequest = 68,
            Padding = new Thickness(20, 0, 12, 0),
            ColumnSpacing = 4,
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Auto),
                new ColumnDefinition(GridLength.Auto)
            }
        };
        bar.Add(titles, 0, 0);
        bar.Add(bell, 1, 0);
        bar.Add(avatar, 2, 0);

        return bar;
    }

    private void Attach()
    {
        Detach();
        _shell = Shell.Current;
        _chrome = Application.Current?.Handler?.MauiContext?.Services.GetService<AppChrome>();
        if (_shell is not null)
        {
            _shell.Navigated += OnNavigated;
            foreach (var tab in Bar?.Items ?? [])
            {
                tab.PropertyChanged += OnTabChanged;
                _watched.Add(tab);
            }
        }

        if (_chrome is not null)
        {
            _chrome.Changed += OnChromeChanged;
            _ = _chrome.RefreshAsync();
        }

        ShowTitle();
        ShowChrome();
        DrawTabs();
    }

    private void Detach()
    {
        if (_shell is not null)
            _shell.Navigated -= OnNavigated;

        foreach (var tab in _watched)
            tab.PropertyChanged -= OnTabChanged;

        if (_chrome is not null)
            _chrome.Changed -= OnChromeChanged;

        _watched.Clear();
        _shell = null;
        _chrome = null;
    }

    private void OnNavigated(object? sender, ShellNavigatedEventArgs args)
    {
        ShowTitle();
        DrawTabs();
    }

    private void OnChromeChanged(object? sender, EventArgs args)
    {
        ShowChrome();
        DrawTabs();
    }

    // A tab shown or hidden for the roles of the user, or its caption in another language.
    private void OnTabChanged(object? sender, PropertyChangedEventArgs args)
    {
        if (args.PropertyName is nameof(ShellSection.IsVisible) or nameof(ShellSection.Title))
            Dispatcher.Dispatch(DrawTabs);
    }

    // The title of the page, or of its tab; the first tab, today, shows the date under it.
    private void ShowTitle()
    {
        var title = (Parent as Page)?.Title;
        var tab = Bar?.CurrentItem;
        _title.Text = string.IsNullOrWhiteSpace(title) ? tab?.Title : title;
        var isToday = tab?.Route == "home";
        _subtitle.Text = isToday ? DateTime.Now.ToString("dddd, d MMMM", CultureInfo.CurrentCulture) : null;
        _subtitle.IsVisible = isToday;
    }

    private void ShowChrome()
    {
        _avatar.Name = _chrome?.UserName;
        _bellBadge.Count = _chrome?.NotificationCount ?? 0;
    }

    // The tabs move when the window crosses a width.
    private void Arrange()
    {
        var mode = Width switch
        {
            <= 0 => _mode,
            < RailWidth => NavigationMode.Bottom,
            < MenuWidth => NavigationMode.Rail,
            _ => NavigationMode.Menu
        };
        if (mode == _mode)
            return;

        _mode = mode;
        DrawTabs();
    }

    private int CountOf(ShellSection tab) => tab.Route switch
    {
        "chats" => _chrome?.ChatCount ?? 0,
        "leads" => _chrome?.LeadCount ?? 0,
        _ => 0
    };

    private void DrawTabs()
    {
        _side.Content = null;
        _bottom.Content = null;
        if (Bar is not { } bar || _mode == NavigationMode.None)
            return;

        var tabs = bar.Items.Where(tab => tab.IsVisible).ToList();
        switch (_mode)
        {
            case NavigationMode.Bottom:
                _bottom.Content = BottomBar(bar, tabs);
                break;
            case NavigationMode.Rail:
                _side.Content = Rail(bar, tabs);
                break;
            default:
                _side.Content = Menu(bar, tabs);
                break;
        }
    }

    private Grid BottomBar(TabBar bar, List<ShellSection> tabs)
    {
        var strip = new Grid { Padding = new Thickness(6, 10, 6, 14), BackgroundColor = ColorOf("Surface") };
        for (var column = 0; column < tabs.Count; column++)
        {
            strip.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
            strip.Add(StackedTab(bar, tabs[column]), column, 0);
        }

        var edge = new BoxView { HeightRequest = 1, Color = ColorOf("Border"), VerticalOptions = LayoutOptions.Start };
        var bottom = new Grid();
        bottom.Add(strip);
        bottom.Add(edge);

        return bottom;
    }

    private Grid Rail(TabBar bar, List<ShellSection> tabs)
    {
        var items = new VerticalStackLayout { Spacing = 18, Padding = new Thickness(0, 16), WidthRequest = 88 };
        items.Add(Logo(showsName: false));
        foreach (var tab in tabs)
            items.Add(StackedTab(bar, tab));

        return Side(items);
    }

    private Grid Menu(TabBar bar, List<ShellSection> tabs)
    {
        var items = new VerticalStackLayout { Spacing = 4, Padding = new Thickness(14, 20), WidthRequest = 248 };
        items.Add(Logo(showsName: true));
        foreach (var tab in tabs)
            items.Add(MenuTab(bar, tab));

        return Side(items);
    }

    // The side of the window under the tabs, with a hairline towards the page.
    private static Grid Side(VerticalStackLayout items)
    {
        var side = new Grid
        {
            BackgroundColor = ColorOf("Surface"),
            ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(new GridLength(1)) }
        };
        side.Add(items, 0, 0);
        side.Add(new BoxView { WidthRequest = 1, Color = ColorOf("Border") }, 1, 0);

        return side;
    }

    // The mark of the school: a gold square with its initial, with the name in a wide window.
    private static HorizontalStackLayout Logo(bool showsName)
    {
        var logo = new HorizontalStackLayout
        {
            Spacing = 12,
            Padding = showsName ? new Thickness(8, 0, 8, 24) : new Thickness(0, 0, 0, 10),
            HorizontalOptions = showsName ? LayoutOptions.Start : LayoutOptions.Center
        };
        var mark = new Border
        {
            WidthRequest = 36,
            HeightRequest = 36,
            StrokeThickness = 0,
            StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(10) },
            BackgroundColor = ColorOf("Accent"),
            Content = new Label
            {
                Text = "A",
                FontFamily = "DisplaySemiBold",
                FontSize = 17,
                TextColor = ColorOf("OnAccent"),
                HorizontalTextAlignment = TextAlignment.Center,
                VerticalTextAlignment = TextAlignment.Center
            }
        };
        logo.Add(mark);
        if (showsName)
        {
            logo.Add(new Label
            {
                Text = "AutoElite",
                FontFamily = "DisplaySemiBold",
                FontSize = 17,
                TextColor = ColorOf("Text"),
                VerticalTextAlignment = TextAlignment.Center
            });
        }

        return logo;
    }

    // A tab of the bottom bar or the rail: its icon in a pill, gold when current, over its caption.
    private VerticalStackLayout StackedTab(TabBar bar, ShellSection tab)
    {
        var isCurrent = tab == bar.CurrentItem;
        var count = CountOf(tab);
        var pill = new Border
        {
            WidthRequest = 60,
            HeightRequest = 32,
            StrokeThickness = 0,
            StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(16) },
            BackgroundColor = isCurrent ? ToneColors.BackgroundOf(StatusTone.Accent) : Colors.Transparent,
            Content = new AppIcon { Name = IconOf(tab), Color = ColorOf(isCurrent ? "AccentText" : "TextMuted") }
        };

        // The count sits over the top right corner of the pill, outside it.
        var icon = new Grid { WidthRequest = 60, HeightRequest = 32, HorizontalOptions = LayoutOptions.Center };
        icon.Add(pill);
        icon.Add(new CountBadge
        {
            Count = count,
            HorizontalOptions = LayoutOptions.End,
            VerticalOptions = LayoutOptions.Start,
            TranslationX = 4,
            TranslationY = -6
        });

        var part = new VerticalStackLayout { Spacing = 4, HorizontalOptions = LayoutOptions.Fill };
        part.Add(icon);
        part.Add(new Label
        {
            Text = tab.Title,
            FontSize = 12,
            FontFamily = "TextMedium",
            MaxLines = 1,
            LineBreakMode = LineBreakMode.TailTruncation,
            HorizontalTextAlignment = TextAlignment.Center,
            TextColor = ColorOf(isCurrent ? "Text" : "TextMuted")
        });

        return Tappable(part, bar, tab, count);
    }

    // A tab of the menu: its icon and caption in a row, on a tint when current, with its count at the end.
    private VerticalStackLayout MenuTab(TabBar bar, ShellSection tab)
    {
        var isCurrent = tab == bar.CurrentItem;
        var count = CountOf(tab);
        var row = new Grid
        {
            HeightRequest = 48,
            Padding = new Thickness(14, 0),
            ColumnSpacing = 14,
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Auto),
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Auto)
            }
        };
        row.Add(new AppIcon { Name = IconOf(tab), Color = ColorOf(isCurrent ? "AccentText" : "TextSecondary") }, 0, 0);
        row.Add(
            new Label
            {
                Text = tab.Title,
                FontSize = 15,
                FontFamily = isCurrent ? "TextSemiBold" : "TextMedium",
                TextColor = ColorOf(isCurrent ? "Text" : "TextSecondary"),
                VerticalTextAlignment = TextAlignment.Center
            },
            1,
            0);
        row.Add(new CountBadge { Count = count }, 2, 0);

        var item = new Border
        {
            StrokeThickness = 0,
            StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(14) },
            BackgroundColor = isCurrent ? ToneColors.BackgroundOf(StatusTone.Accent) : Colors.Transparent,
            Content = row
        };
        var part = new VerticalStackLayout();
        part.Add(item);

        return Tappable(part, bar, tab, count);
    }

    private static string IconOf(ShellSection tab) =>
        tab.Route is { } route && _icons.TryGetValue(route, out var icon) ? icon : "home";

    // A tab opens on a tap; a reader of the screen hears its caption and its count.
    private static VerticalStackLayout Tappable(VerticalStackLayout part, TabBar bar, ShellSection tab, int count)
    {
        SemanticProperties.SetDescription(
            part, count > 0 ? string.Create(CultureInfo.CurrentCulture, $"{tab.Title}, {CountBadge.TextOf(count)}") : tab.Title);
        OnTap(part, () => Routes.GoToAsync($"{bar.Route}/{tab.Route}"));

        return part;
    }
}
